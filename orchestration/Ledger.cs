using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Node
{
    class Ledger 
    {
        public Dictionary<string, MetaNode> Nodes {get; private set;}
        public int Quorum {
            get => ClusterCopy.Count <= 2 ? ClusterCopy.Count : (ClusterCopy.Count / 2) + 2;
        }

        public void AddNode(string name, MetaNode node)
        {
            lock (_cluster_lock)
            {
                Cluster.Add(name, node);
                Logger.Log(this.GetType().Name, "Added " + node.Name + " " + node.Port + " to cluster", Logger.LogLevel.INFO);
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

        public string Vote 
        {
            get 
            {
                // TODO: NOT YET IMPLEMENTED VOTATION METRICS
                return Params.NODE_NAME;
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

        Ledger()
        {
            Nodes = new Dictionary<string, MetaNode>();
        }
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