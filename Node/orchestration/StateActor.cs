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
                    Scheduler.Instance.NextJob();
                    Coordinator.Instance.ResetHeartbeat();
                    break;
                case Coordinator.State.CANDIDATE:
                    ActAsCandidate();
                    break;
                case Coordinator.State.SLEEPING:
                    Scheduler.Instance.NextJob();
                    Coordinator.Instance.ResetHeartbeat();
                    break;
                case Coordinator.State.FOLLOWER:
                    Scheduler.Instance.NextJob();
                    break;
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

        // private string Reschedule(Job job, MetaNode n0)
        // {
        //     string newNode = null;
        //     if (job.TypeOf == Job.Type.SD)
        //     {
        //         newNode = Scheduler.Instance.GetScheduledNode(job, n0.Name);
        //         // newNode = Scheduler.Instance.ScheduleJob(job, n0.Name);
            
        //     } else if (job.TypeOf == Job.Type.OP)
        //     {
        //         // I first reschedule the job a shadow
        //         job.TypeOf = Job.Type.SD;
        //         newNode = Scheduler.Instance.GetScheduledNode(job, n0.Name);
        //         // newNode = Scheduler.Instance.ScheduleJob(job, n0.Name);
        //     }

        //     if (newNode != null)
        //     {
        //         if (job.CounterPart.Node == Params.NODE_NAME)
        //         {
        //             // I find the counterpart, and tell it to run as a operative
        //             Scheduler.Instance.UpdateCounterPart(job.CounterPart.JobId, newNode);

        //         } else 
        //         {
        //             Ledger.Instance.UpdateCounterPart(job.CounterPart.Node, newNode, job.CounterPart.JobId);
        //         }
        //     }

        //     bool status = Scheduler.Instance.ScheduleFinishedJob(newNode, job);

        //     if (status) return newNode;
        //     return Reschedule(job, n0);
        // }

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
                                // If the job was already completed, but dangeling, then skip.
                                if (job.StatusOf == Job.Status.CP) continue;

                                Logger.Log("FailedJob", "A [job"+job.ID+"] has failed on [node:"+n0.Name+"]", Logger.LogLevel.WARN);

                                if (job.CounterPart != null && job.TypeOfRequest == RequestType.SUBSCRIBE && job.TypeOf == Job.Type.OP)
                                {
                                    if (cluster.Count-1 > 0)
                                    {
                                        if (Ledger.Instance.MyContainsPart(job.ID))
                                        {
                                            Logger.Log("ActAsLeader", "Operational [job:"+job.ID +"] failed but I have the counter node (cluster>0)", Logger.LogLevel.INFO);

                                            string shadow = Ledger.Instance.GetShadowMyPart(job.ID);
                                            Scheduler.Instance.TransmitRunAs(shadow);

                                            job.TypeOf = Job.Type.SD;
                                            Scheduler.Instance.ScheduleJob(job);

                                        } else if (Ledger.Instance.AllContainsPart(job.ID))
                                        {
                                            Logger.Log("ActAsLeader", "Operational [job:"+job.ID +"] failed but I found the counter node (cluster>0)", Logger.LogLevel.INFO);
                                            
                                            // send a run as
                                            string shadow = Ledger.Instance.GetShadowAllPart(job.ID);
                                            Ledger.Instance.AddRunAs(shadow);

                                            job.TypeOf = Job.Type.SD;
                                            Scheduler.Instance.ScheduleJob(job);
                                        }
                                    } else 
                                    {
                                        if (Ledger.Instance.MyContainsPart(job.ID))
                                        {
                                            Logger.Log("ActAsLeader", "Operational [job:"+job.ID +"] failed but I have the counter node (cluster<0)", Logger.LogLevel.INFO);

                                            // send a RunAs
                                            string shadow = Ledger.Instance.GetShadowMyPart(job.ID);

                                            
                                            Scheduler.Instance.TransmitRunAs(shadow);

                                            // and schedule a counter job for later
                                            job.TypeOf = Job.Type.SD;
                                            Ledger.Instance.ScheduleCounterJob(job);

                                        } else
                                        {
                                            Scheduler.Instance.ScheduleJob(job);
                                        }
                                        
                                        // Logger.Log("ActAsLeader", "I am alone in the cluster: Queued the job for the next node that becomes part of the mesh", Logger.LogLevel.INFO);
                                        
                                    }
                                } else if (job.CounterPart != null && job.TypeOfRequest == RequestType.SUBSCRIBE && job.TypeOf == Job.Type.SD) 
                                {
                                    if (cluster.Count-1 > 0)
                                    {
                                        Logger.Log("ActAsLeader", "Rescheduling shadow [job:"+job.ID+"] (cluster>0)", Logger.LogLevel.INFO);
                                        Scheduler.Instance.ScheduleJob(job);
                                    } else 
                                    {
                                        Ledger.Instance.ScheduleCounterJob(job);
                                    }
                                }
                                
                                else 
                                {
                                    Logger.Log("ActAsLeader", "Rescheduling [job:"+job.ID+"] (cluster>0)", Logger.LogLevel.INFO);
                                    Scheduler.Instance.ScheduleJob(job);
                                }
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
                        (PlainMetaNode[] Nodes, string[] Remove, Job[] Jobs, (string Node, Job[] nodeJobs)[] _Ledger, (string Node, int nrJobs)[] Facts, Dictionary<string, string> Parts) info = (new PlainMetaNode[]{}, new string[]{}, new Job[]{}, new (string Node, Job[] nodeJobs)[]{}, new (string Node, int nrJobs)[]{}, new Dictionary<string,string>());
                        (string Node, string[] jobs)[] _Sync = new (string Node, string[] jobs)[]{};
                        try 
                        {
                            info = Ledger.Instance.GetNodeUpdates(n0.Key);
                            _Sync = Ledger.Instance.GetSyncRequests(n0.Key);
                        } catch (Exception e)
                        {
                            Logger.Log("Leader:Updates", "[2]" + e.Message, Logger.LogLevel.ERROR);
                        }

                        AppendEntriesRequest ae = new AppendEntriesRequest(){

                            // Get the new nodes, or updated nodes relevant to this node
                            Nodes = info.Nodes,
                            Ledger = info._Ledger,
                            FactSheet = info.Facts,
                            Sync = _Sync,
                            Parts = info.Parts,
                            ScheduledCounter = Ledger.Instance.GetScheduledCounterJobs(),
                            RunAs = Ledger.Instance.GetRunAs(n0.Key),
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


                                List<string> njids = r1.JobIds.ToList();
                                njids.AddRange(r1.CompletedJobs);
                                if (njids.Count > 1) Console.WriteLine(njids.Count);
                                Ledger.Instance.UpdateTemporaryNodes(n0.Key, r1.Ledger, njids.ToArray(), r1.PartIds);// || 1 > Ledger.Instance.GetStatus(n0.Key)));
                                Ledger.Instance.UpdateCompletedJobs(n0.Key, r1.CompletedJobs);
                                Ledger.Instance.UpdateSyncRequests(n0.Key, r1.SyncRequest, r1.SyncResponse);
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
                bool success = Task.WaitAll(tasks, Params.HEARTBEAT_MS);
                // Logger.Log(Params.NODE_NAME, "Send heartbeats [status:"+success+"]", success?Logger.LogLevel.INFO:Logger.LogLevel.WARN);

            }catch(Exception e)
            {
                Logger.Log("ActAsLeader", "[0]" + e.Message, Logger.LogLevel.ERROR);
            }
        
        }        
    }
} 