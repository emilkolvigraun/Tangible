using System.Collections.Generic;
using System.Linq;
using System;

namespace Node
{
    class Ledger 
    {
        private Dictionary<string, MetaNode> Nodes {get;}
        private Dictionary<string, List<string>> _NodesFlags {get;}
        private Dictionary<string, List<string>> _NodesCluster {get;}
        private Dictionary<string, int> NodesStatus {get;}

        public int Quorum {
            get => ClusterCopy.Count <= 2 ? ClusterCopy.Count : (ClusterCopy.Count / 2) + 2;
        }

        public void AddNode(string name, MetaNode node)
        {
            lock (_cluster_lock)
            {
                try
                {
                    if (ContainsKey(name)) 
                    {
                        Cluster[name] = node;
                        Logger.Log(this.GetType().Name, "Added " + node.Name + " to cluster", Logger.LogLevel.INFO);
                    }
                    else Cluster.Add(name, node);
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

        public bool ContainsKey(string key)
        {   
            lock (_cluster_lock)
            {
                return Cluster.ContainsKey(key);
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

        public Job[] GetNodeJobs(string name)
        {
            lock(_nodes_objectives_lock)
            {
                if (Nodes.ContainsKey(name))
                {
                    return Nodes[name].Jobs;
                } else {
                    return new Job[]{};
                }
            }
        }

        public void SetNodesJobs(string name, Job[] jobs)
        {
            if (jobs != null)
            {
                lock (_nodes_objectives_lock)
                {
                    if (Nodes.ContainsKey(name))
                    {
                        Nodes[name].Jobs = jobs;
                    } 
                }
            }
        }

        public string NodeWithLeastJobs()
        {
            try 
            {
                if (Cluster.Count == 0) return null;
                else return Nodes.Aggregate((l, r) => l.Value.Jobs.Length < r.Value.Jobs.Length ? l : r).Key;
            } catch(Exception e)
            {
                Logger.Log("NodesJobs", e.Message, Logger.LogLevel.WARN);
                if (Cluster.Count > 0) return Cluster.ElementAt(0).Key;
                return null;
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
        public Dictionary<string, List<string>> NodesCluster
        {
            get 
            {
                lock(_cluster_nodes_lock)
                {
                    return _NodesCluster;
                }
            }
        }
        public Dictionary<string, List<string>> NodesFlags
        {
            get 
            {
                lock(_cluster_flags_lock)
                {
                    return _NodesFlags;
                }
            }
        }

        public void ResetNodesNCluster(string name)
        {
            bool s1 = false;
            bool s2 = false;
            lock(_cluster_flags_lock)
            {
                if (!_NodesFlags.ContainsKey(name))
                {
                    _NodesFlags.Add(name, new List<string>());
                    s1 = true;
                }
            }
            lock(_cluster_nodes_lock)
            {
                if (!_NodesCluster.ContainsKey(name))
                {
                    _NodesCluster.Add(name, new List<string>());
                    s2 = true;
                }
            }
            if(GetStatus(name) == 0)
            {
                // if hb is 0, then everything has already been sent 
                lock(_cluster_nodes_lock)
                {
                    if(!s2)_NodesCluster[name].Clear();
                }
                lock(_cluster_flags_lock)
                {
                    if(!s1)_NodesFlags[name].Clear();
                }
            }
        }

        public void UpdateNodesCluster(string name, MetaNode node)
        {
            lock(_cluster_nodes_lock)
            {
                if (node == null || node.Name == name) return;

                if (!NodesCluster.ContainsKey(name))
                {
                    NodesCluster.Add(name, new List<string>(){node.Name});
                } else if (!NodesCluster[name].Contains(node.Name))
                {
                    NodesCluster[name].Add(node.Name);
                }
            }
        }

        public void UpdateAllNodesNFlags(string ignore, Dictionary<string, MetaNode> cluster, string flag = null, MetaNode node = null)
        {
            foreach(KeyValuePair<string, MetaNode> n0 in cluster)
            {
                if (n0.Key == ignore) continue;
                lock(_cluster_flags_lock)
                {
                    if (flag != null && !_NodesFlags.ContainsKey(n0.Key))
                    {
                        _NodesFlags.Add(n0.Key, new List<string>(){flag});
                    } else if (flag != null)
                    {
                        _NodesFlags[n0.Key].Add(flag);
                    }
                }
                lock(_cluster_nodes_lock)
                {
                    if (node != null && !_NodesCluster.ContainsKey(n0.Key))
                    {
                        _NodesCluster.Add(n0.Key, new List<string>(){node.Name});
                    } else if (node != null)
                    {
                        _NodesCluster[n0.Key].Add(node.Name);
                    }
                }
            }
        }

        public string[] GetNodesFlags(string name)
        {
            lock(_cluster_flags_lock)
            {
                if (_NodesFlags.ContainsKey(name)) return _NodesFlags[name].ToArray();
                else return new string[]{};
            }
        }

        public MetaNode[] GetNodesCluster(string name)
        {
            lock(_cluster_nodes_lock)
            {
                if (NodesCluster.ContainsKey(name) && Cluster.ContainsKey(name))
                {
                    List<MetaNode> nodes = new List<MetaNode>();
                    foreach(string node in NodesCluster[name])
                    {
                        if (Cluster.ContainsKey(node))
                        {
                            nodes.Add(Cluster[node]);
                        } 
                    }
                    return nodes.ToArray();
                } 
                return new MetaNode[]{};
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
                if (NodesStatus.Copy().ContainsKey(n))
                {
                    NodesStatus[n] = 0;
                } else {
                    NodesStatus.Add(n, 0);
                }
            }
        }

        public bool IfRemove(string n)
        {
            lock (_cluster_state_lock)
            {
                if(GetStatus(n)>30)
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

        Ledger()
        {
            Nodes = new Dictionary<string, MetaNode>();
            NodesStatus = new Dictionary<string, int>();
            _NodesCluster = new Dictionary<string, List<string>>();
            _NodesFlags = new Dictionary<string, List<string>>();
        }
        private static readonly object _nodes_objectives_lock = new object();
        private static readonly object _cluster_flags_lock = new object();
        private static readonly object _cluster_nodes_lock = new object();
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