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
                if (state == Coordinator.State.FOLLOWER || state == Coordinator.State.CANDIDATE) Logger.Log(Params.NODE_NAME, "Acted as " + state.ToString() + " [" + string.Join(",",Ledger.Instance.Cluster.GetAsToString())+"], "+Coordinator.Instance.LeaderHeartbeat.ToString()+"ms, " + Scheduler.Instance.Jobs.Length.ToString(), Logger.LogLevel.INFO);
                else Logger.Log(Params.NODE_NAME, "Acted as " + state.ToString() + " [" + string.Join(",",Ledger.Instance.Cluster.GetAsToString())+"], " + Scheduler.Instance.Jobs.Length.ToString(), Logger.LogLevel.INFO);
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
                        Ledger.Instance.AddNode(item.Name, new MetaNode(){Name=item.Name, Host=item.Host, Port=item.Port});
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
            List<string> flagged = new List<string>();
            Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
            foreach(KeyValuePair<string, MetaNode> n0 in cluster)
            {
                if(Ledger.Instance.IfRemove(n0.Key))
                {
                    flagged.Add(n0.Key);
                    Ledger.Instance.UpdateAllNodesNFlags(n0.Key, cluster, flag:n0.Key);

                    // GET ALL THE JOBS AND SCHEDULE THEM
                    List<Job> jobs = Coordinator.Instance.GetRemoveNewJobs(n0.Key).ToList();
                    jobs.AddRange(Ledger.Instance.GetNodeJobs(n0.Key));
                    Coordinator.Instance.ScheduleJobsToNodes(jobs.ToArray());
                } 
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
                    AppendEntriesRequest ae = new AppendEntriesRequest(){
                        Add = Ledger.Instance.GetNodesCluster(n0.Key),
                        Flag = Ledger.Instance.GetNodesFlags(n0.Key),

                        // Get the jobs assigned to this node
                        // Jobs = Ledger.Instance.GetNodeJobs(n0.Key)
                        Jobs = Coordinator.Instance.GetNewJobs(n0.Key)

                    };

                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, ae, timeout:true);
                    // Console.WriteLine(n0.Key + " " + Ledger.Instance.GetNodesCluster(n0.Key).Length);
                    if (r0.TypeOf == RequestType.AE) 
                    {
                        Ledger.Instance.ResetStatus(n0.Key);
                        Ledger.Instance.ResetNodesNCluster(n0.Key);

                        AppendEntriesRequest response = ((AppendEntriesRequest) r0);

                        if (ae.Flag.Length > 0)
                        {

                        } 
                        List<string> AddResponse = response.Add.GetAsToStringName();
                        foreach (MetaNode n in Ledger.Instance.Cluster.AsNodeArray())
                        {
                            if (!AddResponse.Contains(n.Name))
                            {
                                Ledger.Instance.UpdateNodesCluster(n0.Value.Name, n);
                            }
                        }                       

                        Ledger.Instance.SetNodesJobs(n0.Key, response.Jobs);
                    }
                    // If the jobs were sent, then remove them from the list
                    Coordinator.Instance.UpdateNewJobs(n0.Value.Name);
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