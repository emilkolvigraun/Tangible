using System.Collections.Generic;

namespace Node
{
    class Ledger 
    {
        public Dictionary<string, MetaNode> Nodes {get; private set;}
        public Dictionary<string, int> NodesStatus {get; private set;}
        public int Quorum {
            get => ClusterCopy.Count <= 2 ? ClusterCopy.Count : (ClusterCopy.Count / 2) + 2;
        }

        public void AddNode(string name, MetaNode node)
        {
            lock (_cluster_lock)
            {
                Cluster.Add(name, node);
                Logger.Log(this.GetType().Name, "Added " + node.Name + " to cluster", Logger.LogLevel.INFO);
            }
        }

        public void RemoveNode(string name)
        {
            lock (_cluster_lock)
            {   
                Cluster.Remove(name);
                Logger.Log(this.GetType().Name, "Removed " + name + " from cluster", Logger.LogLevel.INFO);
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
                if(GetStatus(n)>10)
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
        }
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