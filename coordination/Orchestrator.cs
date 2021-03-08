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
        public QuorumNode MyLeader {get; set;} = null;
        public byte[] Certificate { get; private set; }
        private bool Running = false;
        private readonly object qlock = new object();

        public void Init()
        {
            Certificate = AsyncTLSServer.Instance.GetEncodedCertificate();
            Running = true;
            Broadcast();
            KafkaConsumer.Instance.ToggleBroadcastListener();
            KafkaConsumer.Instance.Subscribe();
        }

        // NEED TO MAKE CONSENSUS
        // https://raft.github.io/
        // Consul uses RAFT
        // https://www.consul.io/docs/architecture/consensus
        public void Run()
        {
            while (Running)
            {
                try 
                {
                    int t0 = Convert.ToInt32(Utils.Millis - Raft.Instance.LastHeartbeat);
                    Dictionary<string, QuorumNode> T_Quorum;
                    
                    lock (qlock)
                    {
                        T_Quorum= CopyQuorum();
                    }

                    if (Utils.Millis - Raft.Instance.LastHeartbeat > OrchestrationVariables.HEARTBEAT_MS) 
                    {
                        Console.WriteLine(Raft.Instance.GetState(t0) + ", quorum: " + T_Quorum.Count);
                        Raft.Instance.LastHeartbeat = Utils.Millis;
                    }
                    // if (Utils.Millis - lastHeartbeat > OrchestrationVariables.HEARTBEAT_MS)
                    // {
                    //     Dictionary<string, QuorumNode> T_Quorum = null;
                    //     lock(qlock)
                    //     {
                    //         T_Quorum = GetQuorum().ToDictionary(entry => entry.Key, entry => entry.Value);
                    //     }

                    //     // if (T_Quorum!=null && T_Quorum.Count > 0 && Raft.Instance.GetState()==Raft.Role.CANDIDATE)
                    //     // {
                    //     //     // Console.WriteLine(Raft.Instance.GetState());
                    //     // }
                    // }

                } catch(Exception e)
                {
                    Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR);
                }
            }
        }
        public bool RegisterNode(QuorumNode quorumNode)
        {   
            // lock (qlock)
            // {
                bool status = false;
                try 
                {
                    if (!GetQuorum().ContainsKey(quorumNode.CommonName)){
                        status = true;
                    }
                    GetQuorum()[quorumNode.CommonName] = quorumNode;
                    if(status) Logger.Log(this.GetType().Name, "Registered " + quorumNode.CommonName + " to Quorum of size: " + GetQuorum().Count.ToString(), Logger.LogLevel.INFO);
                    else {
                        foreach(KeyValuePair<string, QuorumNode> q in GetQuorum())
                        {
                            Console.WriteLine(q.Key);
                        }
                    }
                } catch ( Exception e)
                {
                    Logger.Log(this.GetType().Name, "RegisterNode, " + e.Message, Logger.LogLevel.ERROR);
                }
                return status;
            // }
        }
        public void Broadcast()
        {
            Logger.Log(this.GetType().Name, "Broadcasting..", Logger.LogLevel.INFO);
            KafkaProducer.SendMessage(new Request{
                TypeOf = Request.Type.REGISTRATION,
                Node = _Description
            });
        }
        public QuorumNode MakeVote()
        {
            Dictionary<string, QuorumNode> quorum;
            lock (qlock)
            {
                quorum = GetQuorum().ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            QuorumNode node = _Description.AsQuorum();
            float fitness = node.Workload; 
            foreach (KeyValuePair<string, QuorumNode> q in quorum)
            {
                if (q.Value.CommonName.Contains(node.CommonName)) continue;
                else if (q.Value.Workload < fitness) // if burden from start takes lees of a toll on this node
                {
                    node = q.Value;
                    fitness = q.Value.Workload;
                }
            }
            return node;
        }
        public void ContactMemberQuorum(QuorumNode quorumNode)
        {
            lock (qlock)
            {
                Dictionary<string, QuorumNode> T_Quorum = CopyQuorum();
                foreach(KeyValuePair<string, MetaNode> metaNode in quorumNode.Quorum)
                {
                    if (metaNode.Key.Contains(_Description.CommonName) || T_Quorum.ContainsKey(metaNode.Key)) continue;
                    else {
                        RequestHandler.Instance.Registration(
                            new Request(){
                                TypeOf = Request.Type.REGISTRATION,
                                Node = metaNode.Value.AsDescription(metaNode.Key)
                            }
                        );
                        Logger.Log(this.GetType().Name, "Contacted " + metaNode.Key, Logger.LogLevel.INFO);
                    }
                }
            }
        }
        public float CalculateWorkloadBurden()
        {
            var me = Process.GetCurrentProcess();
            return (float)((me.WorkingSet64/ 1024 / 1024)+me.TotalProcessorTime.TotalSeconds);
        }
        public Dictionary<string, QuorumNode> GetQuorum()
        {
            lock (qlock) {
                return _Description.Quorum;
            }
        }
        public Dictionary<string, QuorumNode> CopyQuorum()
        {
            lock(qlock)
            {
                return GetQuorum().ToDictionary(entry => entry.Key, entry => entry.Value);
            }
        }
        public void Dispose()
        {
            Running = false;
        }

        //////////////////////////////////////////////////////////
        ////////////////////////CONSTRUCTOR///////////////////////
        //////////////////////////////////////////////////////////
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




                        
                        // // Reset pulse
                        // if (Utils.Millis - t0 > OrchestrationVariables.HEARTBEAT_S) 
                        // {
                        //     pulsate = true;
                        //     t0 = Utils.Millis;
                        //     Orchestrator.Instance._Description.Workload = CalculateWorkloadBurden();
                        //     if (GetQuorum().Count == 0) Broadcast();
                        // } else {
                        //     pulsate = false;
                        // }

                        // List<string> Flagged = new List<string>();
                        // Dictionary<string, QuorumNode> T_Quorum = GetQuorum().ToDictionary(entry => entry.Key, entry => entry.Value);
                        // foreach(string k0 in T_Quorum.Keys.ToList()) 
                        // {
                        //     QuorumNode N0 = T_Quorum[k0];
                        //     if (N0.HeartBeat >= 5)
                        //     {
                        //         Flagged.Add(k0);
                        //         continue;
                        //     }
                        //     if (pulsate) 
                        //     {
                        //         if (N0.HeartBeat >= 1) 
                        //         {   
                        //             try 
                        //             {
                        //                 NodeClient Client = NodeClient.Connect(N0.AdvertisedHostName, N0.Port, N0.CommonName);
                        //                 if (Client != null) 
                        //                 {
                        //                     Request Response = Client.SendRequestRespondRequest(new Request(){
                        //                         TypeOf = Request.Type.HEARTBEAT,
                        //                         Node = _Description,
                        //                     });
                        //                     RegisterNode(Response.Node.AsQuorum());
                        //                     Logger.Log(this.GetType().Name, "Send heartbeat to "+k0, Logger.LogLevel.ORCHESTRATION);
                        //                 }    
                        //             } catch(Exception)
                        //             {
                        //                 Logger.Log(this.GetType().Name, "Unable to reach "+k0, Logger.LogLevel.ERROR);
                        //                 N0.HeartBeat++;
                        //                 continue;
                        //             }
                                    
                        //         }
                        //         N0.HeartBeat++;
                        //     }

                        //     // Obtain connection to other nodes which I don't have
                        //     if (N0.Quorum.Keys.Count > T_Quorum.Keys.Count)
                        //     {
                        //         foreach(string k1 in N0.Quorum.Keys.ToList()) 
                        //         {
                        //             MetaNode N1 = N0.Quorum[k1];
                        //             if (k1.Contains(_Description.CommonName)) continue;
                        //             RequestHandler.Instance.Registration(
                        //                 new Request(){
                        //                     TypeOf = Request.Type.REGISTRATION,
                        //                     Node = N1.AsDescription(k1)
                        //                 }
                        //             );
                        //         }
                        //     }
                        // }

                        // foreach(string k2 in Flagged)
                        // {
                        //     GetQuorum().Remove(k2);
                        //     Logger.Log(this.GetType().Name, "Removed " + k2 + " from quorum.", Logger.LogLevel.ORCHESTRATION);
                        // }