using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace Node  
{
    class Orchestrator : IDisposable
    {
        public Description _Description {get; set;}
        public byte[] Certificate { get; private set; }
        private bool Running = false;
        private readonly object qlock = new object();

        public void Init()
        {
            Certificate = AsyncTLSServer.Instance.GetEncodedCertificate();
            Running = true;
            KafkaProducer.SendMessage(new Request{
                TypeOf = Request.Type.REGISTRATION,
                Node = _Description
            });

            System.Threading.Tasks.Task.Run(() => {
                System.Threading.Tasks.Task.Delay(3000);
                KafkaConsumer.Instance.ToggleBroadcastListener();
                KafkaConsumer.Instance.Subscribe();
            });
            
        }
        public void Run()
        {
            long t0 = Utils.Millis;
            bool pulsate = false;
            System.Threading.Tasks.Task.Run(() => {
                while (Running)
                {
                    try 
                    {
                        Orchestrator.Instance._Description.Workload = CalculateWorkloadBurden();
                        
                        // Reset pulse
                        if (Utils.Millis - t0 > OrchestrationVariables.HEARTBEAT_S) 
                        {
                            pulsate = true;
                            t0 = Utils.Millis;
                        } else {
                            pulsate = false;
                        }

                        Dictionary<string, QuorumNode> T_Quorum = GetLockQuorum().ToDictionary(entry => entry.Key, entry => entry.Value);
                        foreach(string k0 in T_Quorum.Keys.ToList()) 
                        {
                            QuorumNode N0 = T_Quorum[k0];

                            if (pulsate && N0.HeartBeat >=1) 
                            {
                                NodeClient Client = NodeClient.Connect(N0.AdvertisedHostName, N0.Port, N0.CommonName);
                                if (Client != null) 
                                {
                                    Request Response = Client.SendRequestRespondRequest(new Request(){
                                        TypeOf = Request.Type.HEARTBEAT,
                                        Node = _Description,
                                    });
                                    RegisterNode(Response.Node.AsQuorum());
                                    Logger.Log(this.GetType().Name, "Send heartbeat to "+k0);
                                }
                            }

                            // Obtain connection to other nodes which I don't have
                            if (N0.Quorum.Keys.Count > T_Quorum.Keys.Count)
                            {
                                foreach(string k1 in N0.Quorum.Keys.ToList()) 
                                {
                                    MetaNode N1 = N0.Quorum[k1];
                                    if (k1.Contains(_Description.CommonName)) continue;
                                    RequestHandler.Instance.Registration(
                                        new Request(){
                                            TypeOf = Request.Type.REGISTRATION,
                                            Node = N1.AsDescription(k1)
                                        }
                                    );
                                }
                            }

                            N0.HeartBeat++;
                        }
                    } catch(Exception e)
                    {
                        Logger.Log(this.GetType().Name, e.Message);
                    }
                }
            });
        }
        public bool RegisterNode(QuorumNode quorumNode)
        {   
            bool status = false;
            if (!GetLockQuorum().ContainsKey(quorumNode.CommonName)){
                status = true;
            }
            GetLockQuorum()[quorumNode.CommonName] = quorumNode;
            Logger.Log(this.GetType().Name, "Registered " + quorumNode.CommonName + " to Quorum of size: " + GetLockQuorum().Count.ToString());
            return status;
        }
        public float CalculateWorkloadBurden()
        {
            // Devart.Data.Oracle.EFCore
            // Getting information about current process
            var process = Process.GetCurrentProcess();

            // Preparing variable for application instance name
            var name = string.Empty;

            foreach (var instance in new PerformanceCounterCategory("Process").GetInstanceNames())
            {
                if (instance.StartsWith(process.ProcessName))
                {
                    using (var processId = new PerformanceCounter("Process", "ID Process", instance, true))
                    {
                        if (process.Id == (int)processId.RawValue)
                        {
                            name = instance;
                            break; 
                        }
                    }
                }
            } 

            var cpu = new PerformanceCounter("Process", "% Processor Time", name, true);
            var ram = new PerformanceCounter("Process", "Private Bytes", name, true);

            // Getting first initial values
            cpu.NextValue();
            ram.NextValue();

            return (float)(Math.Round(cpu.NextValue() / Environment.ProcessorCount, 2)+Math.Round(ram.NextValue() / 1024 / 1024, 2));
        }

        public Dictionary<string, QuorumNode> GetLockQuorum()
        {
            lock (qlock) {
                return _Description.Quorum;
            }
        }
        public void Dispose()
        {
            Running = false;
        }

        //////////////////////////////////////////////////////////
        ////////////////////////CONSTRUCTOR///////////////////////
        //////////////////////////////////////////////////////////
        Orchestrator()
        {
        }
        private static Orchestrator _instance = null;
        private static readonly object padlock = new object();
        public static Orchestrator Instance
        {
            get {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new Orchestrator();
                    }
                    return _instance;
                }
            }
        }

    }
}