using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Node
{
    class Ledger 
    {
        public ConcurrentDictionary<string, MetaNode> Cluster {get; private set;}
        public int Quorum {
            get => Cluster.Count <= 2 ? Cluster.Count : (Cluster.Count / 2) + 2;
        }

        public void AddNode(string name, MetaNode node)
        {
            Cluster.AddOrUpdate(name, node, (key, oldValue) => node);
        }

        public string Vote 
        {
            get 
            {
                MetaNode n0 = Utils.ThisAsShallowMetaNode();
                foreach (KeyValuePair<string, MetaNode> n1 in Cluster)
                {
                    if (n1.Value.Usage.AsCeilInt() < n0.Usage) n0 = n1.Value;
                }
                return n0.Name;
            }
        }

        Ledger()
        {
            Cluster = new ConcurrentDictionary<string, MetaNode>();
        }
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