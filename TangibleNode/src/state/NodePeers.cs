using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace TangibleNode
{
    class NodePeers 
    {
        private TDict<string, Peer> _nodes = new TDict<string, Peer>();
        internal ReaderWriterLockSlim _client_lock = new ReaderWriterLockSlim();

        public bool ContainsPeer(string peerID)
        {
            return _nodes.ContainsKey(peerID);
        }

        public void AddNewNode(Node node)
        {
            if (node.ID.Contains(Params.ID)) return;
            bool b = AddIfNew(node);
            if (b)
            {
                _nodes.RemoveThenAdd((l) => l==node.ID, new Dictionary<string, Peer>{{node.ID, new Peer(node)}});
            }
        }

        public int GetHeartbeat(string peerID)
        {   
            if (!_nodes.ContainsKey(peerID)) return 0;
            return _nodes[peerID].Heartbeat.Value;
        }

        public bool AddIfNew(Node node)
        {
            if (node==null || node.ID.Contains(Params.ID)) return false;
            bool b = _nodes.ContainsKey(node.ID);
            if (!b)
            {
                _nodes.AddSafe(new KeyValuePair<string, Peer>(node.ID, new Peer(node)));
                Logger.Write(Logger.Tag.COMMIT, "Comitted APPEND [node:"+node.ID+"] to peers.");
            }
            return b;
        }

        public List<Node> AsNodes
        {
            get 
            {
                List<Node> nodes = new List<Node>();
                _nodes.ForEachRead((p) => {
                    nodes.Add(new Node(){ID = p.Client.ID, Host = p.Client.Host, Port = p.Client.Port});
                });
                return nodes;
            }
        }

        public bool TryGetNode(string ID, out Peer peer)
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

        public void ForEachPeer(System.Action<Peer> action)
        {
            _nodes.Keys.ToList().ForEach((k) => {
                    action(_nodes[k]);
                }   
            );
        }

        public void AppendAction(string id, Action action)
        {
            _nodes[id].AddAction(action);
        }

        private Peer FindAnotherThanToAvoid(string avoid, int retry, KeyValuePair<string, Peer>[] peers)
        {
            Peer peer = peers[retry].Value;
            if (peer.Client.ID == avoid && peers.Length > retry+1)
            {
                return FindAnotherThanToAvoid(avoid, retry+1, peers);
            }
            return peer;
        }

        public string ScheduleAction(string avoid = "")
        {
            int nodeCount = NodeCount;
            if (nodeCount < 1) return Params.ID;
            KeyValuePair<string, Peer>[] peers = new KeyValuePair<string, Peer>[NodeCount];
            _nodes.CopyTo(peers, 0);
            Peer selected_peer = FindAnotherThanToAvoid(avoid, 0, peers);
            if (selected_peer.ActionCount == 0) return selected_peer.Client.ID;
            string selected_peer_name = selected_peer.Client.ID;

            foreach (KeyValuePair<string, Peer> peer in peers)
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

        public void ForEachAsync(System.Action<Peer> action, out Task[] tasks)
        {
            List<Task> t0 = new List<Task>();
            KeyValuePair<string, Peer>[] peers = new KeyValuePair<string, Peer>[NodeCount];
            _nodes.CopyTo(peers, 0);
            foreach(KeyValuePair<string, Peer> peer in peers)
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