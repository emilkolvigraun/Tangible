using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class Ledger 
    {
        // The nodes in the cluster which THIS node knows about
        private Dictionary<string, MetaNode> Nodes {get;} = new Dictionary<string, MetaNode>();
        
        // The nodes which the OTHER nodes knows about
        private Dictionary<string, MetaNode> temp_Nodes {get;} = new Dictionary<string, MetaNode>();

        // If THIS is the Leader: This is the status of all other nodes that THIS node knows about
        private Dictionary<string, int> NodesStatus {get;} = new Dictionary<string, int>();

        private Dictionary<string, (string, string[])[]> SyncRequests {get;} = new Dictionary<string, (string, string[])[]>();

        //                   op  ,  sd
        private Dictionary<string, string> _AllParts {get;} = new Dictionary<string, string>();
        private Dictionary<string, string> MyParts {get;} = new Dictionary<string, string>();

        private HashSet<string> _FinishedJobs = new HashSet<string>();
        private readonly object _finished_jobs_lock = new object();


        public string[] FinishedJobs
        {
            get 
            {
                lock(_finished_jobs_lock)
                {
                    string[] cpj = _FinishedJobs.ToArray();
                    _FinishedJobs.Clear();
                    return cpj;
                }
            }
        }

        public void AddFinishedJob(string job)
        {
            lock(_finished_jobs_lock)
            {
                _FinishedJobs.Add(job);
            }
        }

        public void UpdateCompletedJobs(string n0, string[] cpj)
        {
            HashSet<string> completedJobs = new HashSet<string>(cpj);
            lock(_cluster_lock)
            {
                if (Cluster.ContainsKey(n0))
                {
                    List<Job> newJobs = new List<Job>();
                    foreach(Job j0 in Cluster[n0].Jobs.ToList())
                    {
                        if (!completedJobs.Contains(j0.ID))
                        {
                            newJobs.Add(j0);
                        }
                    }
                    Nodes[n0].Jobs = newJobs.ToArray();
                }
            }
            
            lock(_cluster_entry_lock)
            {
                if(temp_Nodes.ContainsKey(n0))
                {
                    List<Job> newJobs = new List<Job>();
                    foreach(Job j0 in temp_Nodes[n0].Jobs.ToList())
                    {
                        if (!completedJobs.Contains(j0.ID))
                        {
                            newJobs.Add(j0);
                        }
                    }
                    temp_Nodes[n0].Jobs = newJobs.ToArray();
                }
            }
        }


        private Dictionary<string, List<string>> RunAsRequests = new Dictionary<string, List<string>>();
        private readonly object _run_as_lock = new object();

        public string[] GetRunAs(string n0)
        {
            lock(_run_as_lock)
            {
                if (!RunAsRequests.ContainsKey(n0))
                {
                    RunAsRequests.Add(n0, new List<string>()); 
                }

                return RunAsRequests[n0].ToArray();
            }
        }

        public List<string> GetAllJobIds
        {
            get 
            {
                List<string> ids = new List<string>();
                foreach(MetaNode n0 in ClusterCopy.Values)
                {
                    ids.AddRange(n0.Jobs.GetIds());
                }
                ids.AddRange(Scheduler.Instance._Jobs.GetIds());
                return ids;
            }
        }

        private readonly object _my_parts_lock = new object();
        private readonly object _cluster_parts_lock = new object();

        public Dictionary<string, string> AllParts 
        {
            get 
            {
                lock(_cluster_parts_lock)
                {
                    return _AllParts;
                }
            }
        }

        private List<Job> ScheduledCounterJobs = new List<Job>();

        private readonly object _scheduledCounterJobs_lock = new object();

        public bool AllContainsPart(string op)
        {
            lock(_cluster_parts_lock)
            {
                return AllParts.ContainsKey(op);
            }
        }

        public bool MyContainsPart(string op)
        {
            lock(_my_parts_lock)
            {
                return MyParts.ContainsKey(op);
            }
        }

        public string GetShadowAllPart(string op)
        {
            lock(_cluster_parts_lock)
            {
                if (AllContainsPart(op)) return AllParts[op];
                else return null;
            }
        }

        public string GetShadowMyPart(string op)
        {
            lock(_my_parts_lock)
            {
                if (MyContainsPart(op))
                {
                    Logger.Log("GetMyPart", "Found: [job:"+MyParts[op]+"]", Logger.LogLevel.INFO);
                    return MyParts[op];
                } else return null;
            }
        }

        
        // Always add counter parts to all 
        public void AddToAllParts(string op, string sd)
        {
            lock(_cluster_parts_lock)
            {
                if (!_AllParts.ContainsKey(op))
                    _AllParts.Add(op, sd);
                else 
                    _AllParts[op] = sd;
            }
        }

        // Keep track of the counter parts that I am in charge of
        public void AddToMyParts(string op, string sd)
        {
            lock(_my_parts_lock)
            {
                if (!MyParts.ContainsKey(op))
                    MyParts.Add(op, sd);
                else 
                    MyParts[op] = sd;
            }
        }


        // This should be integrated with the info logic
        public void ScheduleCounterJob(Job job)
        {
            lock(_scheduledCounterJobs_lock)
            {
                if (!ScheduledCounterJobs.Any(j => j.ID == job.ID))
                {
                    ScheduledCounterJobs.Add(job);
                    Logger.Log("ScheduleCounter", "Scheduled counter job for when a new node appears", Logger.LogLevel.INFO);
                }
            }
        }
        public Job[] GetScheduledCounterJobs()
        {
            lock(_scheduledCounterJobs_lock)
            {
                if (ScheduledCounterJobs.Count > 0)
                    Logger.Log("ScheduleCounter", "Retrieved scheduled counterjobs", Logger.LogLevel.INFO);
                return ScheduledCounterJobs.ToArray();
            }
        }
        public void ClearScheduledCounterJobs()
        {
            lock(_scheduledCounterJobs_lock)
            {
                ScheduledCounterJobs.Clear();
            }
        }

        // only ever used by followers
        public void SetScheduledCounterJobs(Job[] jobs)
        {
            lock(_scheduledCounterJobs_lock)
            {
                ScheduledCounterJobs = jobs.ToList();
            }
        }

        // Cluster stuff
        public int Quorum {
            get => ClusterCopy.Count <= 2 ? ClusterCopy.Count : (ClusterCopy.Count / 2) + 2;
        }
        public void AddNode(string name, MetaNode node)
        {
            lock (_cluster_lock)
            {
                try
                {
                    if (Cluster.ContainsKey(name)) 
                    {
                        Cluster[name] = node;
                    }
                    else Cluster.Add(name, node);
                    Logger.Log(this.GetType().Name, "Added " + node.Name + " to cluster: " + string.Join(",",Cluster.GetAsToString()), Logger.LogLevel.INFO);
                } catch (Exception e)
                {
                    Logger.Log("AddNode", e.Message, Logger.LogLevel.ERROR);
                }
            }
        }
        public void RemoveNode(string name)
        {
            lock (_cluster_lock)
            {   try
                {
                    Cluster.Remove(name);
                    Logger.Log(this.GetType().Name, "Removed " + name + " from cluster", Logger.LogLevel.INFO);
                } catch (Exception e)
                {
                    Logger.Log("RemoveNode", e.Message, Logger.LogLevel.ERROR);
                }
            }
        }
        public Dictionary<string, MetaNode> Cluster
        {
            get 
            {
                lock(_cluster_lock)
                {
                    return Nodes;
                }
            }
        }
        public Dictionary<string, MetaNode> ClusterCopy
        {
            get 
            {
                lock(_cluster_lock)
                {
                    return Cluster.Copy();
                }
            }
        }
        public void IncrementAll()
        {
            lock (_cluster_state_lock)
            {
                foreach (KeyValuePair<string, int> ns in NodesStatus.Copy())
                {
                    NodesStatus[ns.Key] = NodesStatus[ns.Key]+1;
                }
            }
        }
        public int GetStatus(string n)
        {
            lock (_cluster_state_lock)
            {
                if (NodesStatus.ContainsKey(n))
                {
                    return NodesStatus[n];
                } else {
                    NodesStatus.Add(n, 0);
                    return 0;
                }
            }
        }
        public void ResetStatus(string n)
        {
            lock (_cluster_state_lock)
            {
                if (NodesStatus.ContainsKey(n))
                {
                    NodesStatus[n] = 0;
                } else {
                    NodesStatus.Add(n, 0);
                }

                lock(_run_as_lock)
                {
                    if (RunAsRequests.ContainsKey(n))
                    {
                        RunAsRequests[n].Clear();
                    }
                }
            }
        }

        public void AddRunAs(string sd)
        {
            lock(_cluster_lock)
            {
                foreach (string n0 in Cluster.Keys)
                {
                    lock(_run_as_lock)
                    {
                        if (!RunAsRequests.ContainsKey(n0))
                            RunAsRequests.Add(n0, new List<string>());
                        
                        RunAsRequests[n0].Add(sd);
                    }
                }
            }
        }

        ///<summary>
        ///<para>returns true if the node is unhealthy</para>
        ///<para>if true: removed node from cluster</para>
        ///<para>if false: kept</para>
        ///</summary>
        public bool ValidateIfRemove(string n)
        {
            lock (_cluster_state_lock)
            {
                if(GetStatus(n)>Params.TIMEOUT_LIMIT)
                {
                    lock(_cluster_lock)
                    {
                        RemoveNode(n);
                    }
                    NodesStatus.Remove(n);
                    return true;
                }
                return false;
            }
        }

        public (string, string[])[] GetSyncRequests(string node)
        {
            lock(_sync_lock)
            {
                if (!SyncRequests.ContainsKey(node))
                {
                    SyncRequests.Add(node, new (string, string[])[]{});
                }
                return SyncRequests[node];
            }
        }

        public void UpdateSyncRequests(string node, (string node, string[] jobs)[] request, (string node, Job[] jobs)[] response)
        {
            if (response.Length > 0)
            {
                lock (_sync_lock) lock (_cluster_entry_lock) lock (_cluster_lock)
                {
                    foreach((string node, Job[] jobs) res in response)
                    {
                        foreach(Job j0 in res.jobs)
                        {
                            if (temp_Nodes.ContainsKey(node))
                            {
                                if(!temp_Nodes[node].Jobs.Any(j => j.ID == j0.ID))
                                {
                                    int nr_jobs = temp_Nodes[node].Jobs.Length;
                                    List<Job> Jobs = temp_Nodes[node].Jobs.ToList();
                                    Jobs.Add(j0);
                                    temp_Nodes[node].Jobs = Jobs.ToArray();
                                }
                            }
                            AddJob(node, j0);
                        }
                        RemoveSyncRequest(node, res.node);
                    }
                }
            }
            if (request.Length > 0)
            {
                lock(_sync_lock) lock(_cluster_lock)
                {
                    List<string> missing = new List<string>();
                    foreach ((string node, string[] jobs) req in request)
                    {
                        if (Cluster.ContainsKey(req.node))
                        {
                            foreach (string j in req.jobs)
                            {
                                if(!Cluster[req.node].Jobs.ContainsKey(j))
                                {
                                    missing.Add(j);
                                }
                            }
                            if (SyncRequests.ContainsKey(node))
                            {
                                (string n, string[] j)[] c = SyncRequests[node];
                                (string n, string[] j)[] c1 = c.Append((req.node, missing.ToArray())).ToArray();
                                SyncRequests[node] = c1;
                            } else 
                            {
                                SyncRequests.Add(node, new (string, string[])[]{(req.node, missing.ToArray())});
                            }
                        }
                    }
                }
            }
        }

        private void RemoveSyncRequest(string n0, string n1)
        {
            lock(_sync_lock)
            {
                if (SyncRequests.ContainsKey(n0))
                {
                    List<(string node, string[] jobs)> r = new List<(string node, string[] jobs)>();
                    foreach((string node, string[] jobs) d in SyncRequests[n0])
                    {
                        if (d.node != n1)
                        {
                            r.Add(d);
                        }
                    }  
                    SyncRequests[n0] = r.ToArray();
                }

            }
        }

        public (string node, string[] jobs)[] ValidateFactSheet((string node, int nrjobs)[] sheet)
        {
            List<(string node, string[] jobs)> syncRequest= new List<(string node, string[] jobs)>();
            
            lock (_cluster_lock)
            {
                foreach ((string node, int nrjobs) fact in sheet)
                {
                    if(Cluster.ContainsKey(fact.node) && Cluster[fact.node].Jobs.Length != fact.nrjobs)
                    {
                        string[] noCompleteIds = Cluster[fact.node].Jobs.GetIdsIfNotComplete();
                        if (noCompleteIds.Length > 0) syncRequest.Add((fact.node, noCompleteIds));
                    }
                }
            }
            if (syncRequest.Count > 0)
            {
                Logger.Log("FactSheet", "Detected SYNC error", Logger.LogLevel.WARN);
            }
            return syncRequest.ToArray();
        }

        public (string node, Job[] jobs)[] RespondWithJobs((string node, string[] jobs)[] syncJobs)
        {
            List<(string node, Job[] jobs)> newJobs = new List<(string node, Job[] jobs)>();

            lock(_cluster_lock)
            {
                foreach((string node, string[] jobs) request in syncJobs)
                {
                    if(Cluster.ContainsKey(request.node))
                    {
                        List<Job> jobs = new List<Job>();
                        foreach(string j0 in request.jobs)
                        {
                            Job j1 = Cluster[request.node].Jobs.GetJob(j0);
                            if (j1 != null) jobs.Add(j1);
                        }
                        newJobs.Add((request.node, jobs.ToArray()));
                    }
                }
            }
            return newJobs.ToArray();
        }

        // When the leader receives a response, it stores the current state of the node
        public void UpdateTemporaryNodes(string n0, (string node, string[] jobIds)[] nodes, string[] jobs, string[] parts)
        {
            lock (_cluster_entry_lock)
            {
                Dictionary<string, MetaNode> tempCluster;
                lock (_cluster_lock)
                {
                    tempCluster = ClusterCopy;
                }

                List<string> jobIds = jobs.ToList();
                List<Job> newJobs = new List<Job>();
                Job[] _jobs = tempCluster[n0].Jobs;
                foreach(Job j0 in _jobs)
                {
                    if (jobIds.Contains(j0.ID))
                    {
                        newJobs.Add(j0);
                    } 
                }
                List<(string node, string[] jobIds)> nodeIds = nodes.ToList();
                List<BasicNode> newNodes = new List<BasicNode>();
                List<BasicNode> _nodes = tempCluster.AsNodeArray().AsBasicNodes();

                Dictionary<string, string> newParts = new Dictionary<string, string>();
                Dictionary<string, string> allParts = Ledger.Instance.AllParts.Copy();
                foreach (string part in parts)
                {
                    if (allParts.ContainsKey(part))
                    {
                        newParts.Add(part, allParts[part]);
                    }
                }

                // Add a copy of the basic node that this node knows about
                // but only with the jobs that the other node knows about
                // such that tempNode reflects the current state of information 
                // that each other node in the cluster contains
                foreach(BasicNode b0 in _nodes)
                {
                    if (nodeIds.ContainsNode(b0.ID))
                    {
                        if (b0.JobEquality(nodeIds.GetJobIds(b0.ID)))
                        {
                            newNodes.Add(b0);   
                        } else 
                        {
                            BasicNode b1 = b0.Copy(nodeIds.GetJobIds(b0.ID));
                            newNodes.Add(b1);   
                        }
                    } 
                }

                if (nodeIds.ContainsNode(Params.UNIQUE_KEY))
                {
                    BasicNode b1 = BasicNode.MakeBasicNode(new MetaNode()).Copy(nodeIds.GetJobIds(Params.UNIQUE_KEY));
                    newNodes.Add(b1);
                }
                
                if (temp_Nodes.ContainsKey(n0))
                {
                    temp_Nodes[n0].Nodes = newNodes.ToArray();
                    temp_Nodes[n0].Jobs = newJobs.ToArray();
                } else 
                {
                    MetaNode n1 = tempCluster[n0].Copy();
                    n1.Nodes = newNodes.ToArray();
                    n1.Jobs = newJobs.ToArray();
                    n1.Parts = newParts;
                    temp_Nodes.Add(n0, n1);
                }
            }
        }
        public (PlainMetaNode[] Nodes, string[] Remove, Job[] Jobs, (string Node, Job[] nodeJobs)[] _Ledger, (string Node, int nrJobs)[] Facts, Dictionary<string, string> parts) GetNodeUpdates(string n0)
        {
            lock(_cluster_entry_lock)
            {

                Dictionary<string, MetaNode> tempCluster;
                lock (_cluster_lock)
                {
                    // whatever information that this node has about all other nodes in the cluster
                    tempCluster = ClusterCopy;
                }

                
                List<(string Node, int nrJobs)> _facts = new List<(string Node, int nrJobs)>();                
                // update fact sheet
                try 
                {
                    foreach(MetaNode nn in tempCluster.Values)
                    {
                        _facts.Add((nn.Name, nn.Jobs.Length));
                    }
                    _facts.Add((Params.NODE_NAME, Scheduler.Instance._Jobs.Length));
                } catch(Exception e)
                {
                    Logger.Log("TemperaryNodes", "[1] " + e.Message, Logger.LogLevel.ERROR);
                }

                // If temp nodes includes the key, then we have done this before
                // if temp cluster does not contain the key, then something is wrong
                if (temp_Nodes.ContainsKey(n0) && tempCluster.ContainsKey(n0))
                {
                    // Init the lists to return as arrays
                    List<PlainMetaNode> _nodes = new List<PlainMetaNode>();
                    List<string> _remove = new List<string>();
                    List<Job> _jobs = new List<Job>();    
                    List<(string Node, Job[] nodeJobs)> _ledger = new List<(string Node, Job[] nodeJobs)>();
                    Dictionary<string, string> _parts = new Dictionary<string, string>();
                    // the current contents and status of the node in question
                    List<BasicNode> tempNodes = temp_Nodes[n0].Nodes.ToList();

                    try 
                    {
                        
                        foreach(KeyValuePair<string, string> p0 in Ledger.Instance.AllParts.Copy())
                        {
                            if (!temp_Nodes[n0].Parts.ContainsKey(p0.Key) || temp_Nodes[n0].Parts[p0.Key] != p0.Value)
                            {
                                _parts.Add(p0.Key, p0.Value);
                            }

                            if (_parts.Count >= 4) break;
                        }


                        // add node to _nodes if it is not already added to tempNodes
                        foreach(MetaNode n1 in tempCluster.Values)
                        {
                            // Adds the node, if it is not the same node as this
                            // if it is not already added
                            if ( n0 != n1.Name && !tempNodes.ContainsKey(n1.Name))
                            {
                                _nodes.Add(PlainMetaNode.MakePlainMetaNode(n1));
                            }
                            // Never send more than 4 new jobs at the time
                            if (_nodes.Count >= 4) break;
                        }

                        // foreach node in tempNodes (which reflects the current state of information that the other node has)
                        // we check if it information is lacking
                        foreach(BasicNode b1 in tempNodes)
                        {
                            Job[] extractedJobs = new Job[]{};
                            
                            if (b1.Name == Params.NODE_NAME)
                            {
                                List<Job> _current_jobs = new List<Job>();
                                foreach(Job j1 in Scheduler.Instance._Jobs)
                                {
                                    if (!b1.Jobs.ContainsKey(j1))
                                    {
                                        _current_jobs.Add(j1);
                                    }
                                    if (_current_jobs.Count >= 4)
                                    {
                                        break;
                                    }
                                }
                                extractedJobs = _current_jobs.ToArray();
                            }
                            else if (tempCluster.ContainsKey(b1.Name)) 
                            {
                                extractedJobs = ExtractLackingJobs(b1, tempCluster[b1.Name]);
                            }

                            if (extractedJobs.Length > 0)
                            {
                                _ledger.Add((b1.Name, extractedJobs));
                                if (extractedJobs.Length >= 4) break;
                            }
                        }
                    } catch(Exception e)
                    {
                        Logger.Log("TemperaryNodes", "[0] " + e.Message, Logger.LogLevel.ERROR);
                    }

                    try 
                    {
                        foreach(BasicNode n1 in tempNodes)
                        {
                            if (n1.Name != Params.NODE_NAME && !Cluster.ContainsKey(n1.Name))
                            {
                                _remove.Add(n1.Name);
                            }
                        } 
                    } catch(Exception e)
                    {
                        Logger.Log("TemperaryNodes", "[2] " + e.Message, Logger.LogLevel.ERROR);
                    }

                    try 
                    {
                        List<Job> tempJobs = temp_Nodes[n0].Jobs.ToList();
                        foreach(Job job in tempCluster[n0].Jobs)
                        {
                            if (!tempJobs.ContainsKey(job))
                            {
                                _jobs.Add(job);
                            } 

                            // never send more than 4 jobs at the time
                            if (_jobs.Count >= 4) break;
                        }
                    } catch(Exception e)
                    {
                        Logger.Log("TemperaryNodes", "[3] " + e.Message, Logger.LogLevel.ERROR);
                    }
                    return (_nodes.ToArray(), _remove.ToArray(), _jobs.ToArray(), _ledger.ToArray(), _facts.ToArray(), _parts);
                } else { 
                    Logger.Log("NodeUpdates", "Did not contain [node:"+n0+"]", Logger.LogLevel.WARN);
                    // (PlainMetaNode[] Nodes, Job[] Jobs) info = GetNodesAndJobs(n0);

                    // correct this
                    return (new PlainMetaNode[]{}, new string[]{}, new Job[]{}, new (string Node, Job[] nodeJobs)[]{}, _facts.ToArray(), new Dictionary<string,string>());
                }
            }
        }

        public Job[] ExtractLackingJobs(BasicNode b0, MetaNode n0)
        {
            List<Job> _current_jobs = new List<Job>();
            foreach(Job j1 in n0.Jobs)
            {
                if (!b0.Jobs.ContainsKey(j1))
                {
                    _current_jobs.Add(j1);
                }
                if (_current_jobs.Count >= 4)
                {
                    break;
                }
            }
            return _current_jobs.ToArray();
        }
        
        // When the follower receives the Nodes and Remove
        public void UpdateNodes((string Node, Job[] jobs)[] _ledger, PlainMetaNode[] nodes, string[] remove, Dictionary<string, string> parts)
        {
            lock (_cluster_lock)
            {
                foreach(PlainMetaNode n0 in nodes)
                {
                    try
                    {
                        int temp = Cluster.Count;
                        if (!Cluster.ContainsKey(n0.Name))
                        {
                            Cluster.Add(n0.Name, PlainMetaNode.MakeMetaNode(n0));
                            Logger.Log("UpdateNodes", "Received cluster APPEND: [node:" + n0.ID + "]", Logger.LogLevel.IMPOR);
                        }
                    } catch(Exception e)
                    {
                        Logger.Log("UpdateNodes", "[0] " + e.Message, Logger.LogLevel.ERROR);
                    }
                }
                
                try 
                {
                    foreach (MetaNode n0 in Cluster.Values)
                    {
                        Job[] jobs = _ledger.GetJobs(n0.Name);
                        if (jobs.Length > 0)
                        {
                            foreach(Job j0 in jobs)
                            {
                                AddJob(n0.Name, j0);
                                Logger.Log("UpdateNodes", "Received job APPEND: [node:" + n0.ID + ", [job:" + j0.ID + "]", Logger.LogLevel.IMPOR);
                            }
                        }
                    }
                } catch (Exception e)
                {
                        Logger.Log("UpdateNodes", "[1] " + e.Message, Logger.LogLevel.ERROR);
                }

                foreach(string s0 in remove)
                {
                    try
                    {
                        if (Cluster.ContainsKey(s0))
                        {
                            Cluster.Remove(s0);
                            Logger.Log("UpdateNodes", "Received cluster REMOVE: [node:" + s0 + "]", Logger.LogLevel.IMPOR);
                        }

                    } catch(Exception e)
                    {
                        Logger.Log("UpdateNodes", "[2] " + e.Message, Logger.LogLevel.ERROR);
                    }
                }

                foreach(KeyValuePair<string, string> part in parts)
                {
                    try
                    {
                        AddToAllParts(part.Key, part.Value);

                    } catch(Exception e)
                    {
                        Logger.Log("UpdateNodes", "[2] " + e.Message, Logger.LogLevel.ERROR);
                    }
                }
            }
        }

        // add job to follower node
        public bool AddJob(string node, Job job)
        {
            lock(_cluster_lock)
            {
                if (Cluster.ContainsKey(node) && !Cluster[node].Jobs.Any(j => j.ID == job.ID))
                {
                    int nr_jobs = Cluster[node].Jobs.Length;
                    List<Job> Jobs = Cluster[node].Jobs.ToList();
                    Jobs.Add(job);
                    Cluster[node].Jobs = Jobs.ToArray();
                    if (Cluster[node].Jobs.Length > nr_jobs) Logger.Log("Schedule/AddJob", "Assigned [job:" + job.ID + "] to [node:" + node + "]", Logger.LogLevel.IMPOR);
                    return true;
                } 
                return false;
            }
        }
        private static readonly object _counter_part_lock = new object();
        private static readonly object _sync_lock = new object();
        private static readonly object _cluster_entry_lock = new object();
        private static readonly object _cluster_state_lock = new object();
        private static readonly object _cluster_lock = new object();
        private static readonly object _lock = new object();
        private static Ledger _instance = null;
        public static Ledger Instance 
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null) _instance = new Ledger();
                    return _instance;
                }
            }
        }
    }
}