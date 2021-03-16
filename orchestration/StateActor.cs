using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Node
{
    class StateActor
    {

        private long _updater = Utils.Millis;

        public void Process(Coordinator.State state)
        {
            switch(state)
            {
                case Coordinator.State.LEADER:
                    ActAsLeader();
                    break;
                case Coordinator.State.CANDIDATE:
                    ActAsCandidate();
                    break;
                case Coordinator.State.FOLLOWER:
                    ActAsFollower();
                    break;
                default:
                    break;
            }

            if (_updater+5000 < Utils.Millis)
            {
                Logger.Log(Params.NODE_NAME, "Acted as " + state.ToString() + " quorum: " + string.Join(",",Ledger.Instance.ClusterCopy.GetAsToString()), Logger.LogLevel.INFO);
                _updater = Utils.Millis;
            }
        }

        private void ActAsFollower()
        {
            Change item = Coordinator.Instance.DequeueChange();
            while (item != null)
            {
                if (item.TypeOf == Change.Type.ADD)
                {
                    Ledger.Instance.AddNode(item.Name, new MetaNode(){Name=item.Name, Host=item.Host, Port=item.Port});
                } else if (item.TypeOf == Change.Type.DEL)
                {
                    Ledger.Instance.RemoveNode(item.Name);
                }
                item = Coordinator.Instance.DequeueChange();
            }
        }

        private void ActAsCandidate()
        {
            
            try 
            {
                // Starting the Election term
                Coordinator.Instance.StartElectionTerm();

                // Retrieving a copy of all the nodes within the cluster
                Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;

                var tasks = new Task[cluster.Count];

                if (Coordinator.Instance.CurrentLeader != null && cluster.Count > 1)
                {
                    tasks = new Task[cluster.Count-1];
                }

                try 
                {
                    int i = 0;

                    // Sending a Vote request to all known nodes
                    foreach(KeyValuePair<string, MetaNode> n0 in cluster)
                    {
                        try 
                        {
                            if (n0.Key == Coordinator.Instance.CurrentLeader) continue;
                        } catch(Exception e)
                        {
                            Logger.Log("election term 3", e.Message, Logger.LogLevel.ERROR);
                        }

                        tasks[i] = new Task(() =>
                        {
                            try 
                            {
                                IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, RequestType.VT);
                                if (r0.TypeOf == RequestType.VT && ((VotingRequest)r0).Vote != Params.NODE_NAME )//&& n0.Key != Coordinator.Instance.CurrentLeader)
                                {
                                    Coordinator.Instance.StopElectionTerm();
                                }
                            }
                            catch(Exception e){Logger.Log("election term 4", e.Message, Logger.LogLevel.ERROR);}
                            });
                        i++;
                    }
                } catch(Exception e)
                {
                    Logger.Log("election term 1", e.Message, Logger.LogLevel.ERROR);
                }
                
                try {
                    // Try to send heartbeat to all nodes in parallel
                    Parallel.ForEach<Task>(tasks, (t) => { t.Start(); }); 
                    Task.WaitAll(tasks);
                }catch(Exception e)
                {
                    Logger.Log("election term 2", e.Message, Logger.LogLevel.ERROR);
                }
                
                if (Coordinator.Instance.IsElectionTerm) Coordinator.Instance.ToggleLeadership(true);
                else Coordinator.Instance.ToggleLeadership(false);

                // Resetting heartbeat (also works as a penalty)
                Coordinator.Instance.ResetHeartbeat();

            } catch(Exception e)
            {
                Logger.Log("election term 0", e.Message, Logger.LogLevel.ERROR);
            }    
        }

        private void ActAsLeader()
        {
            Ledger.Instance.IncrementAll();
            List<string> flagged = new List<string>();
            foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
            {
                if(Ledger.Instance.IfRemove(n0.Key)) flagged.Add(n0.Key);
            }

            // Retrieve a copy of all the nodes in the cluster
            Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
            
            // create the append entries request, to make sure that all nodes receive the same
            AppendEntriesRequest ar = new AppendEntriesRequest(){
                Add = cluster.AsNodeArray(),
                Flag = flagged.ToArray()
            };
            SendBatch(ar, cluster);
        }


        private void SendBatch(IRequest request, Dictionary<string, MetaNode> cluster)
        {
            // create a task for each node
            var tasks = new Task[cluster.Count];
            int i = 0;
            foreach(KeyValuePair<string, MetaNode> n0 in cluster)
            {
                tasks[i] = new Task(() =>
                {
                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, request, timeout:true);
                    if (r0.TypeOf == RequestType.AE) Ledger.Instance.ResetStatus(n0.Key);
                });
                i++;
            }

            // Try to send heartbeat to all nodes in parallel
            Parallel.ForEach<Task>(tasks, (t) => { t.Start(); }); 
            bool success = Task.WaitAll(tasks, 500);
            // Logger.Log(Params.NODE_NAME, "Send heartbeats [status:"+success+"]", success?Logger.LogLevel.INFO:Logger.LogLevel.WARN);
        }
    }
} 