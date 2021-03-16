using System.Collections.Generic;
using System;

namespace Node
{
    class Ledger 
    {
        public Dictionary<string, MetaNode> Nodes {get; private set;}
        public Dictionary<string, List<string>> _NodesFlags {get; private set;}
        public Dictionary<string, List<MetaNode>> _NodesCluster {get; private set;}
        public Dictionary<string, int> NodesStatus {get; private set;}
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
        public Dictionary<string, List<MetaNode>> NodesCluster
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
            bool s = false;
            lock(_cluster_flags_lock)
            {
                if (!_NodesFlags.ContainsKey(name))
                {
                    _NodesFlags.Add(name, new List<string>());
                    s = true;
                }
            }
            lock(_cluster_nodes_lock)
            {
                if (!_NodesCluster.ContainsKey(name))
                {
                    _NodesCluster.Add(name, new List<MetaNode>());
                    s = true;
                }
            }
            if (s) return;
            if(GetStatus(name) == 0)
            {
                // if hb is 0, then everything has already been sent 
                lock(_cluster_nodes_lock)
                {
                    _NodesCluster[name].Clear();
                }
                lock(_cluster_flags_lock)
                {
                    _NodesFlags[name].Clear();
                }
            }
        }

        public void UpdateNodesCluster(string name, MetaNode node)
        {
            lock(_cluster_nodes_lock)
            {
                if (node != null && !_NodesCluster.ContainsKey(name))
                {
                    _NodesCluster.Add(name, new List<MetaNode>(){node});
                } else if (node != null)
                {
                    _NodesCluster[name].Add(node);
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
                        _NodesCluster.Add(n0.Key, new List<MetaNode>(){node});
                    } else if (node != null)
                    {
                        _NodesCluster[n0.Key].Add(node);
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
                if (_NodesCluster.ContainsKey(name)) return _NodesCluster[name].ToArray();
                else return new MetaNode[]{};
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
                if (NodesStatus.Copy().ContainsKey(n))
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
            _NodesCluster = new Dictionary<string, List<MetaNode>>();
            _NodesFlags = new Dictionary<string, List<string>>();
        }
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