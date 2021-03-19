using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

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

            if (_updater+4000 < Utils.Millis)
            {
                if (state == Coordinator.State.FOLLOWER || state == Coordinator.State.CANDIDATE) Logger.Log(Params.NODE_NAME, "Acted as " + state.ToString() + " [" + string.Join(",",Ledger.Instance.Cluster.GetAsToString())+"], "+Coordinator.Instance.LeaderHeartbeat.ToString()+"ms, " + Scheduler.Instance._Jobs.Length.ToString(), Logger.LogLevel.INFO);
                else Logger.Log(Params.NODE_NAME, "Acted as " + state.ToString() + " [" + string.Join(",",Ledger.Instance.Cluster.GetAsToString())+"], " + Scheduler.Instance._Jobs.Length.ToString(), Logger.LogLevel.INFO);
                _updater = Utils.Millis;
            }
        }

        private void ActAsFollower()
        {
            try 
            {
                Change item = Coordinator.Instance.ChangeQueue.DequeueChange();
                while (item != null)
                {
                    if (item.TypeOf == Change.Type.ADD)
                    {
                        Ledger.Instance.AddNode(item.Name, new MetaNode(){Name=item.Name, Host=item.Host, Port=item.Port, Jobs=item.Jobs});
                    } else if (item.TypeOf == Change.Type.DEL)
                    {
                        Ledger.Instance.RemoveNode(item.Name);
                    }
                    item = Coordinator.Instance.ChangeQueue.DequeueChange();
                }

                // EXECUTE JOBS
            } catch(Exception e)
            {
                Logger.Log("ActAsFollower", e.Message, Logger.LogLevel.ERROR);
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

                var tasks = new Task[]{};

                try 
                {
                    // Sending a Vote request to all known nodes
                    foreach(KeyValuePair<string, MetaNode> n0 in cluster)
                    {
                        if (n0.Key == Coordinator.Instance.CurrentLeader) continue;

                        tasks.Append(new Task(() =>
                        {
                            try 
                            {
                                IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, RequestType.VT);
                                if (r0.TypeOf == RequestType.VT && ((VotingRequest)r0).Vote != Params.NODE_NAME )//&& n0.Key != Coordinator.Instance.CurrentLeader)
                                {
                                    Coordinator.Instance.StopElectionTerm();
                                }
                            }
                            catch(Exception e){Logger.Log("election term 3", e.Message, Logger.LogLevel.ERROR);}
                            }));
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
            Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
            foreach(KeyValuePair<string, MetaNode> n0 in cluster)
            {
                Ledger.Instance.ValidateIfRemove(n0.Key);
            }

            // Retrieve a copy of all the nodes in the cluster 
            cluster = Ledger.Instance.ClusterCopy;
                        
            // create a task for each node
            var tasks = new Task[cluster.Count];
            int i = 0;
            foreach(KeyValuePair<string, MetaNode> n0 in cluster)
            {
                tasks[i] = new Task(() =>
                {
                    (MetaNode[] Nodes, string[] Remove, Job[] Jobs) info = Ledger.Instance.GetNodeUpdates(n0.Key);
                    AppendEntriesRequest ae = new AppendEntriesRequest(){

                        // Get the new nodes, or updated nodes relevant to this node
                        Nodes = info.Nodes,

                        // Get the nodes that are flagged for deletion
                        Remove = info.Remove,

                        // Get the jobs assigned to this node
                        Jobs = info.Jobs

                    };

                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, ae, timeout:true);
                    if (r0.TypeOf == RequestType.AR) 
                    {
                        Ledger.Instance.ResetStatus(n0.Key);
                        AppendEntriesResponse r1 = ((AppendEntriesResponse) r0);
                        Ledger.Instance.UpdateTemporaryNodes(n0.Value.Name, r1.Node);
                    }
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