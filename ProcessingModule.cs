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
        Dictionary<string, (string Node, string Topic)> Associations;
        Dictionary<string, string> LatestValues;
        object Lock;
        object valueLock;

        public EdgeClient Client {get; private set;}

        public ProcessingModule(EdgeClient client)
        {
            Client = client;
            Subscribers = new Dictionary<string, List<string>>();
            Associations = new Dictionary<string, (string Node, string Topic)>();
            LatestValues = new Dictionary<string, string>();
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

        private bool ValidateReceivedValue(string id, string currentValue)
        {
            lock (valueLock) 
            {
                string value;
                if (LatestValues.TryGetValue(id, out value))
                {
                    if (value.Equals(currentValue))
                    {
                        return false;
                    }
                    else 
                    {
                        LatestValues[id] = currentValue;
                        return true;
                    }
                }
                else 
                {
                    LatestValues.Add(id, currentValue);
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
            if (!ValidateReceivedValue(_src[1], _value)) return true;

            if (_src.Length > 1 && _src[0].ToString().Equals("opcua"))
            {
                lock (Lock)
                {
                    List<string> returnTopics;
                    if (Subscribers.TryGetValue(_src[1].ToString(), out returnTopics)){
                        foreach(string top in returnTopics)
                        {
                            string header = "protocol: datasphere-ingest 0.1\n\n";
                            
                            List<List<string>> Data = new List<List<string>>();
                            Data.Add(new List<string> {data.Split(" ")[1].Split(":")[1], _value.Split(":")[1]});

                            Dictionary<string, string> MetaData = new Dictionary<string, string>();
                            MetaData.Add("date/time/format", "epoch/ms");
                            MetaData.Add("date/value/type", "float");
                            MetaData.Add("source/type", _src[0]);
                            MetaData.Add("source/instance", _src[1]);
                            MetaData.Add("identifier", GetHashString(source));

                            WesleyMessage msg = new WesleyMessage {
                                data = Data,
                                metadata = MetaData
                            };

                            string payload = header + JsonConvert.SerializeObject(msg, Formatting.Indented);
                            
                            // Client.KafkaSendMessage(id:"softwsdu", topic:top, message:string.Format("{0} {1} {2} {3}", source, data, debugging1, debugging2));
                            Client.KafkaSendMessage(id:"softwsdu", topic:top, message:payload);
                            Client.Log("Processed update to " + top);
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
                    string rtopic = cmd[4];
                    try 
                    {
                        string id = Client.OPCUAStartSubscription(url, node, this);
                        if (id != null) {
                            Client.KafkaSendMessage(id:"softwsdu", topic:rtopic, message:FormatResponse("start","id",id));
                            lock (Lock) {
                                if (Subscribers.ContainsKey(node))
                                {
                                    if (!Subscribers[node].Contains(rtopic))
                                    {
                                        Subscribers[node].Add(rtopic);
                                        Associations[id] = (node, rtopic);
                                    }
                                } else {
                                    Subscribers.Add(node, new List<string>{rtopic});
                                    Associations.Add(id, (node, rtopic));
                                }
                            }
                        } 
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
                        bool success = Client.OPCUAStopSubscription(id:id);
                        (string Node, string Topic) topic; 
                        bool error = false;
                        lock (Lock) {
                            error = Associations.TryGetValue(id, out topic);
                            if(error)
                            {
                                if (success)
                                {
                                    Subscribers[topic.Node].Remove(topic.Topic);
                                    Associations.Remove(id);
                                    Client.KafkaSendMessage(id:"softwsdu", topic:topic.Topic, message:FormatResponse("stop","status","success"));
                                } else {
                                    Client.KafkaSendMessage(id:"softwsdu", topic:topic.Topic, message:FormatResponse("stop","status","error"));
                                }
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