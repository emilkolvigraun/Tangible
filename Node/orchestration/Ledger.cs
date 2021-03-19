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
        public void ValidateIfRemove(string n)
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
                }
            }

        }


        // When the leader receives a response, it stores the current state of the node
        public void UpdateTemporaryNodes(string n0, MetaNode node)
        {
            lock (_cluster_entry_lock)
            {
                if (temp_Nodes.ContainsKey(n0))
                {
                    temp_Nodes[n0] = node;
                } else {
                    temp_Nodes.Add(n0, node);
                }
            }
        }
        public (MetaNode[] Nodes, string[] Remove, Job[] Jobs) GetNodeUpdates(string n0)
        {
            lock(_cluster_entry_lock)
            {

                Dictionary<string, MetaNode> tempCluster;
                lock (_cluster_lock)
                {
                    tempCluster = ClusterCopy;
                }
                // If temp nodes includes the key, then we have done this before
                // if temp cluster does not contain the key, then something is wrong
                if (temp_Nodes.ContainsKey(n0) && tempCluster.ContainsKey(n0))
                {
                    // Init the lists to return as arrays
                    List<MetaNode> _nodes = new List<MetaNode>();
                    List<string> _remove = new List<string>();
                    List<Job> _jobs = new List<Job>();    

                    List<BasicNode> tempNodes = temp_Nodes[n0].Nodes.ToList();
                    foreach(MetaNode n1 in tempCluster.Values)
                    {
                        // Adds the node, if it is not the same node as this
                        // if it is not already added
                        // or if there is a difference in the jobs
                        if ( n0 != n1.Name && (!tempNodes.ContainsKey(n1.Name) || n1.Jobs.Length != temp_Nodes[n1.Name].Jobs.Length))
                        {
                            _nodes.Add(n1);
                        }
                    }

                    // if the leader node as received a new job
                    // or if the follower node for some reason does not have the leader node
                    // add it
                    BasicNode leaderNode = tempNodes.GetByName(Params.NODE_NAME);
                    if (leaderNode == null || leaderNode.Jobs.Length != Scheduler.Instance._Jobs.Length)
                    {
                        _nodes.Add(new MetaNode());
                    }

                    foreach(BasicNode n1 in tempNodes)
                    {
                        if (n1.Name != Params.NODE_NAME && !Cluster.ContainsKey(n1.Name))
                        {
                            _remove.Add(n1.Name);
                        }
                    } 
                    List<Job> tempJobs = temp_Nodes[n0].Jobs.ToList();
                    foreach(Job job in tempCluster[n0].Jobs)
                    {
                        if (!tempJobs.ContainsKey(job))
                        {
                            _jobs.Add(job);
                        }
                    }

                    return (_nodes.ToArray(), _remove.ToArray(), _jobs.ToArray());
                } else { 
                    (MetaNode[] Nodes, Job[] Jobs) info = GetNodesAndJobs(n0);
                    return (info.Nodes, new string[]{}, info.Jobs);
                }
            }
        }
        private (MetaNode[] Nodes, Job[] Jobs) GetNodesAndJobs(string n0)
        {
            // Init empty list and array
            List<MetaNode> _nodes = new List<MetaNode>();
            Job[] _jobs = new Job[]{};
        
            // Iterate the cluster
            foreach (MetaNode node in ClusterCopy.Values)
            {
                // If a node in the cluster is not the same as n0
                // add it to the list
                if (node.Name != n0) _nodes.Add(node);
            }
            // if the node is in the cluster, return its jobs
            if (Cluster.ContainsKey(n0))
            {
                _jobs = Cluster[n0].Jobs;
            }
            // return all other nodes than the one asking
            // and the jobs of the one asking
            return (_nodes.ToArray(), _jobs);
        }

        // When the follower receives the Nodes and Remove
        public void UpdateNodes(MetaNode[] nodes, string[] remove)
        {
            lock (_cluster_lock)
            {
                foreach(MetaNode n0 in nodes)
                {
                    if (Cluster.ContainsKey(n0.Name))
                    {
                        Cluster[n0.Name] = n0;
                    } else Cluster.Add(n0.Name, n0);
                }
                foreach(string s0 in remove)
                {
                    if (Cluster.ContainsKey(s0))
                    {
                        Cluster.Remove(s0);
                    }
                }
            }
        }

        // add job to follower node
        public bool AddJob(string node, Job job)
        {
            lock(_cluster_lock)
            {
                if (Cluster.ContainsKey(node) && !Nodes[node].Jobs.Any(j => j.ID == job.ID))
                {
                    List<Job> Jobs = Nodes[node].Jobs.ToList();
                    Jobs.Add(job);
                    Nodes[node].Jobs = Jobs.ToArray();
                    Logger.Log("Schedule/AddJob", "Assigned [job:" + job.ID + "] to [node:" + node + "]", Logger.LogLevel.IMPOR);
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