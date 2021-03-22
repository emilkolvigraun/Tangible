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

        // When the leader receives a response, it stores the current state of the node
        public void UpdateTemporaryNodes(string n0, (string node, string[] jobIds)[] nodes, string[] jobs, bool updateSelf)
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

                // if (!updateSelf && temp_Nodes.ContainsKey(n0) && temp_Nodes[n0].Nodes.ContainsID(Params.UNIQUE_KEY))
                // {
                //     BasicNode nx = temp_Nodes[n0].Nodes.GetByID(Params.UNIQUE_KEY);
                //     newNodes.Add(nx);
                // } else 
                // {
                //     newNodes.Add(BasicNode.MakeBasicNode(new MetaNode()));
                // }  
                
                if (temp_Nodes.ContainsKey(n0))
                {
                    temp_Nodes[n0].Nodes = newNodes.ToArray();
                    temp_Nodes[n0].Jobs = newJobs.ToArray();
                } else 
                {
                    MetaNode n1 = tempCluster[n0].Copy();
                    n1.Nodes = newNodes.ToArray();
                    n1.Jobs = newJobs.ToArray();
                    temp_Nodes.Add(n0, n1);
                }
            }
        }
        public (PlainMetaNode[] Nodes, string[] Remove, Job[] Jobs, (string Node, Job[] nodeJobs)[] _Ledger) GetNodeUpdates(string n0)
        {
            lock(_cluster_entry_lock)
            {

                Dictionary<string, MetaNode> tempCluster;
                lock (_cluster_lock)
                {
                    // whatever information that this node has about all other nodes in the cluster
                    tempCluster = ClusterCopy;
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

                    // the current contents and status of the node in question
                    List<BasicNode> tempNodes = temp_Nodes[n0].Nodes.ToList();

                    // if the leader node as received a new job
                    // or if the follower node for some reason does not have the leader node
                    // add it
                    // try 
                    // {
                    //     BasicNode leaderNode = tempNodes.GetByName(Params.NODE_NAME);
                    //     if (leaderNode == null)// || leaderNode.Jobs.Length != Scheduler.Instance._Jobs.Length)
                    //     {
                    //         _nodes.Add(new PlainMetaNode(){
                    //             ID = Params.UNIQUE_KEY,
                    //             Host = Params.ADVERTISED_HOST_NAME,
                    //             Name = Params.NODE_NAME,
                    //             Port = Params.PORT_NUMBER
                    //         });
                    //     }
                    // } catch(Exception e)
                    // {
                    //     Logger.Log("TemperaryNodes", "[1] " + e.Message, Logger.LogLevel.ERROR);
                    // }

                    try 
                    {
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
                    return (_nodes.ToArray(), _remove.ToArray(), _jobs.ToArray(), _ledger.ToArray());
                } else { 
                    Logger.Log("NodeUpdates", "Did not contain [node:"+n0+"]", Logger.LogLevel.WARN);
                    // (PlainMetaNode[] Nodes, Job[] Jobs) info = GetNodesAndJobs(n0);

                    // correct this
                    return (new PlainMetaNode[]{}, new string[]{}, new Job[]{}, new (string Node, Job[] nodeJobs)[]{});
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
        public void UpdateNodes((string Node, Job[] jobs)[] _ledger, PlainMetaNode[] nodes, string[] remove)
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