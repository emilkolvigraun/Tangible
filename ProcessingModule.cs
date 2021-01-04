using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace EC.MS
{
    public class ProcessingModule
    {

        Dictionary<string, List<string>> Subscribers;
        Dictionary<string, string> Associations;
        Dictionary<string, string> Ids;
        Dictionary<string, (string value, string timestamp)> LatestValues;

        object Lock;
        object valueLock;

        public EdgeClient Client {get; private set;}

        public ProcessingModule(EdgeClient client)
        {
            Client = client;
            Subscribers = new Dictionary<string, List<string>>();
            Associations = new Dictionary<string, string>();
            LatestValues = new Dictionary<string, (string value, string timestamp)>();
            Ids = new Dictionary<string, string>();

            Lock = new object();
            valueLock = new object();
        }

        public bool OnDataReceived(string source, string data)
        {
            string[] sourceInfo = source.Split(":");
            if (sourceInfo[0].Contains("kafka")) return ProcessCommand(data);
            else if (sourceInfo[0].Contains("opcua")) return ProcessUpdate(source, data);
            else {
                Client.Log(string.Format("Unable to recognize source: ", source));
                return false;
            }
        }

        private bool ValidateReceivedValue(string id, string currentValue, string currentTimeStamp)
        {
            lock (valueLock) 
            {
                (string value, string timestamp) value;
                if (LatestValues.TryGetValue(id, out value))
                {
                    if (value.Equals(currentValue))
                    {
                        return false;
                    }
                    else 
                    {
                        LatestValues[id] = (currentValue, currentTimeStamp);
                        return true;
                    }
                }
                else 
                {
                    LatestValues.Add(id, (currentValue, currentTimeStamp));
                    return true;
                }
            }
        }

        private bool ProcessUpdate(string source, string data)
        {
            Client.Log("Received OPC update: "+ source + " " + data);
            string debugging1 = "driver_received:"+DefaultLog.CurrentTimeMillis();
            string[] _src = source.Split(":");
            string _value = data.Split(" ")[0];
            if (!ValidateReceivedValue(_src[1], data.Split(" ")[1].Split(":")[1], _value.Split(":")[1])) return true;

            Client.Log(string.Format("Received value: {0}", _value));

            if (_src.Length > 1 && _src[0].ToString().Equals("opcua"))
            {
                lock (Lock)
                {
                    List<string> returnTopics;
                    if (Subscribers.TryGetValue(_src[1].ToString(), out returnTopics)){
                        foreach(string top in returnTopics)
                        {
                            Task.Run(() => {
                                Client.Log(string.Format("Checking for value: {0} to {1}", _src[1].ToString(), top));
                                SendUpdate(timestamp:data.Split(" ")[1].Split(":")[1], value:_value.Split(":")[1], topic:top.ToString(), source:_src[0], _src[1]);
                                Client.Log("Processed update to " + top + ", topic: " + top.ToString());
                            });
                        }
                    } 
                    else 
                    {
                        Client.Log(string.Format("Found no return topics for subscription to: {0}", _src[1]));
                        return false;
                    }
                }
            }
            return true;
        }

        private void SendUpdate(string timestamp, string value, string topic, string source, string instance)
        {
            string header = "protocol: datasphere-ingest 0.1\n\n";
            List<List<string>> Data = new List<List<string>>();
            Data.Add(new List<string> {timestamp, value});

            Dictionary<string, string> MetaData = new Dictionary<string, string>();
            MetaData.Add("date/time/format", "epoch/ms");
            MetaData.Add("date/value/type", "float");
            MetaData.Add("source/type", source);
            MetaData.Add("source/instance", instance);
            MetaData.Add("identifier", GetHashString(source));

            WesleyMessage msg = new WesleyMessage {
                data = Data,
                metadata = MetaData
            };

            string payload = header + JsonConvert.SerializeObject(msg, Formatting.Indented);
            
            // Client.KafkaSendMessage(id:"softwsdu", topic:top, message:string.Format("{0} {1} {2} {3}", source, data, debugging1, debugging2));
            Client.KafkaSendMessage(id:"softwsdu", topic:topic, message:payload);
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private string FormatResponse(string method, string tag, string message)
        {
            return string.Format("response {0} {1}:{2}", method, tag, message);
        }

        private bool ProcessCommand(string command)
        {
            Client.Log(string.Format("Received command: : {0}", command));

            string[] cmd = command.Split(" ");

            if (cmd[0].ToLower().Equals("subscribe") && cmd.Length >= 3){
                if (cmd[1].ToLower().Equals("begin") && cmd.Length == 5) 
                {
                    string url = cmd[2];
                    string node = cmd[3];
                    string subscription = node;
                    string rtopic = cmd[4];
                    try 
                    {
                        string id;
                        bool success = Ids.TryGetValue(subscription, out id);

                        if (!success) 
                        {
                            Client.Log("Creating new OPC UA subscription.");
                            id = Client.OPCUAStartSubscription(url, node, this);
                            Ids.Add(subscription, id);
                        } else {
                            Client.Log("Attaching subscriber to known OPC UA subscription.");
                            (string t, string v) val;
                            bool vs = LatestValues.TryGetValue(node, out val);
                            if (vs) SendUpdate(val.t, val.v, rtopic, "opcua", node);
                        }

                        if (Subscribers.ContainsKey(subscription)){
                            if (!Subscribers[subscription].Contains(rtopic)){
                                Subscribers[subscription].Add(rtopic);
                                Associations[id] = subscription;
                            }
                        } else 
                        {
                            Subscribers.Add(subscription, new List<string>{rtopic});
                            Associations.Add(id, subscription);
                        }
                        
                        if (id != null) Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:FormatResponse("start","id",id));
                        else Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:"error");
                    }
                    catch(Exception ex)
                    {
                        Client.Log(string.Format("Begin exception: {0}", ex.Message));
                        Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:"error");
                    }
                }
                if (cmd[1].ToLower().Equals("stop"))
                {
                    if (cmd.Length == 3)
                    {
                        string id = cmd[2];
                        string rtopic = cmd[3];
                        string subscription; 
                        bool error = false;
                        lock (Lock) {
                            error = Associations.TryGetValue(id, out subscription);
                            if(error)
                            {
                                Subscribers[subscription].Remove(rtopic);

                                if (Subscribers.Count() < 1) 
                                {
                                    Associations.Remove(id);
                                    bool success = Client.OPCUAStopSubscription(id:id);
                                    if (success)
                                    {
                                        Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:FormatResponse("stop","status","success stopping "+subscription.ToString()));
                                    } else {
                                        Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:FormatResponse("stop","status","error stopping "+subscription.ToString()));
                                    }
                                }
                                Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:FormatResponse("stop","status","success stopping "+subscription.ToString()));                                
                            }
                        }
                    } 
                } 
            }
            if (cmd[0].ToLower().Equals("write")) 
            {
                try
                {
                    string url = cmd[1];
                    string node = cmd[2];
                    string value = cmd[3];
                    bool success = Client.OPCUAWriteToNode(url, node, value);

                    List<string> returnTopics;

                    lock (Lock) 
                    {
                        if (Subscribers.TryGetValue(node, out returnTopics))
                        {
                            foreach(string top in returnTopics)
                            {
                                if (success) Client.KafkaSendMessage(id:"softwsdu", topic:top, message:FormatResponse("write","status","success"));
                                else Client.KafkaSendMessage(id:"softwsdu", topic:top, message:FormatResponse("write","status","error"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                { Client.Log(string.Format("Write exception: {0}", ex.Message));}
            } 

            return true;
        }
    }
}