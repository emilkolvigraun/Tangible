using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace TangibleNode
{
    class Nodes 
    {
        private TDict<string, Node> _nodes = new TDict<string, Node>();
        internal ReaderWriterLockSlim _client_lock = new ReaderWriterLockSlim();

        public bool ContainsPeer(string peerID)
        {
            return _nodes.ContainsKey(peerID);
        }

        public void AddNewNode(Sender node)
        {
            if (node.ID.Contains(Params.ID)) return;
            bool b = AddIfNew(node);
            if (b)
            {
                _nodes.RemoveThenAdd((l) => l==node.ID, new Dictionary<string, Node>{{node.ID, new Node(node)}});
            }
        }

        public int GetHeartbeat(string peerID)
        {   
            if (!_nodes.ContainsKey(peerID)) return 0;
            return _nodes[peerID].Heartbeat.Value;
        }

        public bool AddIfNew(Sender node)
        {
            if (node==null || node.ID.Contains(Params.ID)) return false;
            bool b = _nodes.ContainsKey(node.ID);
            if (!b)
            {
                _nodes.AddSafe(new KeyValuePair<string, Node>(node.ID, new Node(node)));
                Logger.Write(Logger.Tag.COMMIT, "Comitted APPEND [node:"+node.ID+"] to peers.");
            }
            return b;
        }

        public List<Sender> AsSenders
        {
            get 
            {
                List<Sender> nodes = new List<Sender>();
                _nodes.ForEachRead((p) => {
                    nodes.Add(new Sender(){ID = p.Client.ID, Host = p.Client.Host, Port = p.Client.Port});
                });
                return nodes;
            }
        }

        public bool TryGetNode(string ID, out Node peer)
        {
            bool b = _nodes.ContainsKey(ID);
            if (!b)
            {
                peer = null;
                return false;
            }
            peer = _nodes[ID];
            return true;
        }
        public bool TryRemoveNode(string ID)
        {
            bool b = _nodes.ContainsKey(ID);
            if (!b)
            {
                return false;
            } 
            _nodes.Remove(ID);
            Logger.Write(Logger.Tag.COMMIT, "Committed REMOVE [node:"+ID+"] from peers.");
            return true;
        }
        public int NodeCount
        {
            get 
            {
                return _nodes.Count;
            }
        }

        public int PeerLogCount 
        {
            get 
            {
                int entries = _nodes.Count;
                ForEachPeer((p) => {
                    entries += p.ActionCount;
                });
                return entries;
            }
        }

        public void ForEachPeer(System.Action<Node> action)
        {
            _nodes.Keys.ToList().ForEach((k) => {
                    action(_nodes[k]);
                }   
            );
        }

        public bool AppendRequest(string id, DataRequest action)
        {
            if (_nodes.ContainsKey(id))
            {
                _nodes[id].AddAction(action);
                return true;
            }
            return false;
        }

        private Node FindAnotherThanToAvoid(string avoid, int retry, KeyValuePair<string, Node>[] peers)
        {
            Node peer = peers[retry].Value;
            if (peer.Client.ID == avoid && peers.Length > retry+1)
            {
                return FindAnotherThanToAvoid(avoid, retry+1, peers);
            }
            return peer;
        }

        public string ScheduleRequest(string avoid = "")
        {
            int nodeCount = NodeCount;
            if (nodeCount < 1) return Params.ID;
            KeyValuePair<string, Node>[] peers = new KeyValuePair<string, Node>[NodeCount];
            _nodes.CopyTo(peers, 0);
            Node selected_peer = FindAnotherThanToAvoid(avoid, 0, peers);
            if (selected_peer.ActionCount == 0) return selected_peer.Client.ID;
            string selected_peer_name = selected_peer.Client.ID;

            foreach (KeyValuePair<string, Node> peer in peers)
            {
                if (peer.Value.Client.ID == avoid) continue;
                if (peer.Value.ActionCount < selected_peer.ActionCount)
                {
                    selected_peer_name = peer.Value.Client.ID;
                    selected_peer = peer.Value;
                }
            }

            if (selected_peer_name == avoid) return Params.ID;

            return selected_peer_name;
        }

        /// <summary>
        /// For accessing and doing something with the heatbeat (TInt)
        /// <para>Example arguments: ("abc", (h) => h.Increment())</para>
        /// </summary>
        public void AccessHeartbeat(string ID, System.Action<TInt> action)
        {
            if (_nodes.ContainsKey(ID))
                action(_nodes[ID].Heartbeat);
        }

        public void ForEachAsync(System.Action<Node> action, out Task[] tasks)
        {
            List<Task> t0 = new List<Task>();
            KeyValuePair<string, Node>[] peers = new KeyValuePair<string, Node>[NodeCount];
            _nodes.CopyTo(peers, 0);
            foreach(KeyValuePair<string, Node> peer in peers)
            {
                t0.Add(
                    new Task(()=>{
                        action(peer.Value);
                    })
                );
            }
            tasks = t0.ToArray();
        }
    }
}