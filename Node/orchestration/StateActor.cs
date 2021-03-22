using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Node 
{
    class StateActor
    {
        public void Process(Coordinator.State state)
        {
            switch(state)
            {
                case Coordinator.State.LEADER:
                    ActAsLeader();
                    Coordinator.Instance.ResetHeartbeat();
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
        }

        private void ActAsFollower()
        {
            try 
            {
                
                // EXECUTE AND MAINTAIN JOBS

            } catch(Exception e)
            {
                Logger.Log("ActAsFollower", e.Message, Logger.LogLevel.ERROR);
            }
        }

        private void ActAsCandidate()
        {
            try 
            {
                // Start current election term
                Coordinator.Instance.StartElectionTerm();

                // Retrieving a copy of all the nodes within the cluster
                Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;

                // Init a list of tasks to reach out to all other nodes in parallel
                List<Task> tasks = new List<Task>();

                // Create vote request
                VotingRequest vote = new VotingRequest();

                foreach(MetaNode n0 in cluster.Values)
                {
                    try 
                    {
                        if (n0.Name == Coordinator.Instance.CurrentLeader || !Coordinator.Instance.IsElectionTerm) continue;
                        tasks.Add(new Task(() => {
                            IRequest r0 = Coordinator.Instance.GetClient(n0.Name).Run(n0.Host, n0.Port, n0.Name, vote, timeout:true);
                            
                            // NodeClient.RunClient(n0.Host, n0.Port, n0.Name, vote, timeout:true);
                    
                            if (r0.TypeOf == RequestType.VT && ((VotingRequest)r0).Vote != vote.Vote)
                            {
                                Coordinator.Instance.StopElectionTerm();
                            }

                            Logger.Log("ActAsCandidate", "Send vote to [node:" + n0.Name+"]", Logger.LogLevel.INFO);
                        }));
                    } catch(Exception e)
                    {
                        Logger.Log("ActAsCandidate", "[1] " + e.Message, Logger.LogLevel.ERROR);
                    }
                }

                Task[] _tasks = tasks.ToArray();
                Parallel.ForEach<Task>(_tasks, (t) => { t.Start(); });
                Task.WaitAll(_tasks, 400);
                
                if (Coordinator.Instance.IsElectionTerm) Coordinator.Instance.ToggleLeadership(true);
                else Coordinator.Instance.ToggleLeadership(false);

                // Reset heartbeat
                Coordinator.Instance.ResetHeartbeat();
            } catch(Exception e)
            {
                Logger.Log("ActAsCandidate", "[0] " + e.Message, Logger.LogLevel.ERROR);
            }
            
            Logger.Log("ActAsCandidate", "Acted as candidate", Logger.LogLevel.INFO);
        }

        private void ActAsLeader()
        {
            try 
            {
                Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
                
                try 
                {
                    Ledger.Instance.IncrementAll();
                    foreach(MetaNode n0 in cluster.Values)
                    {
                        bool status = Ledger.Instance.ValidateIfRemove(n0.Name);
                        if (status)
                        {
                            foreach(Job job in n0.Jobs)
                            {
                                Scheduler.Instance.ScheduleJob(job);
                            }
                        }
                    }
                } catch(Exception e)
                {
                    Logger.Log("Leader:IfRemove", "[1]" + e.Message, Logger.LogLevel.ERROR);
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
                        (MetaNode[] Nodes, string[] Remove, Job[] Jobs) info = (new MetaNode[]{}, new string[]{}, new Job[]{});
                        try 
                        {
                            info = Ledger.Instance.GetNodeUpdates(n0.Key);
                        } catch (Exception e)
                        {
                            Logger.Log("Leader:Updates", "[2]" + e.Message, Logger.LogLevel.ERROR);
                        }

                        AppendEntriesRequest ae = new AppendEntriesRequest(){

                            // Get the new nodes, or updated nodes relevant to this node
                            Nodes = info.Nodes,

                            // Get the nodes that are flagged for deletion
                            Remove = info.Remove,

                            // Get the jobs assigned to this node
                            Jobs = info.Jobs

                        };

                        IRequest r0 = Coordinator.Instance.GetClient(n0.Value.Name).Run(n0.Value.Host, n0.Value.Port, n0.Value.Name, ae, timeout:true);
                        if (r0.TypeOf == RequestType.AR) 
                        {
                            try 
                            {
                                Ledger.Instance.ResetStatus(n0.Key);
                            } catch(Exception e)
                            {
                                Logger.Log("Leader:Reset", "[3]" + e.Message, Logger.LogLevel.ERROR);
                            }
                            try 
                            {
                                AppendEntriesResponse r1 = ((AppendEntriesResponse) r0);
                                Ledger.Instance.UpdateTemporaryNodes(n0.Value.Name, r1.NodeIds, r1.JobIds, info.Nodes.ContainsKey(Params.NODE_NAME));// || 1 > Ledger.Instance.GetStatus(n0.Key)));
                            } catch(Exception e)
                            {
                                Logger.Log("Leader:Nodes", "[4]" + e.Message, Logger.LogLevel.ERROR);
                            }
                        }
                    });
                    i++;
                }

                // Try to send heartbeat to all nodes in parallel
                Parallel.ForEach<Task>(tasks, (t) => { t.Start(); }); 
                bool success = Task.WaitAll(tasks, 400);
                // Logger.Log(Params.NODE_NAME, "Send heartbeats [status:"+success+"]", success?Logger.LogLevel.INFO:Logger.LogLevel.WARN);

            }catch(Exception e)
            {
                Logger.Log("ActAsLeader", "[0}" + e.Message, Logger.LogLevel.INFO);
            }
        
        }        
    }
} 