using System.Collections.Generic;
using System.Linq;

namespace Node 
{
    class Cluster 
    {

        private Dictionary<string, NodeClient> _clients = new Dictionary<string, NodeClient>();
        private Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
        private readonly object _client_lock = new object();
        private Dictionary<string, int> _heartbeats = new Dictionary<string, int>();

        public List<NodeClient> Clients
        {
            get 
            {
                lock(_client_lock)
                {
                    return _clients.Values.ToList();
                }
            }
        }
        public Node GetNode(string id)
        {
            lock(_client_lock)
            {
                if (_nodes.ContainsKey(id))
                {   
                    return _nodes[id];
                }
                Logger.Log("GetNode", "Node does not exist", Logger.LogLevel.WARN);
                return null;
            }
        }
        public NodeClient GetClient(string id)
        {
            lock(_client_lock)
            {
                if (_clients.ContainsKey(id))
                {   
                    return _clients[id];
                }
                Logger.Log("GetClient", "Client does not exist", Logger.LogLevel.WARN);
                return null;
            }
        }
        public NodeClient AddGetClient(Node node)
        {
            lock(_client_lock)
            {
                AddClient(node);
                return _clients[node.Key];
            }
        }
        public void AddClient(Node node)
        {
            lock(_client_lock)
            {
                if (!_clients.ContainsKey(node.Key))
                    _clients.Add(node.Key, new NodeClient(node.Host, node.Name, node.Port));
                else _clients[node.Key] = new NodeClient(node.Host, node.Name, node.Port);

                if (!_nodes.ContainsKey(node.Key))
                    _nodes.Add(node.Key, node);
                else _nodes[node.Key] = node;

                Logger.Log("AddClient", "attached node " + node.Name + ", " + node.Key, Logger.LogLevel.INFO);
            }
        }
        public List<string> ClientIds
        {
            get 
            {
                lock(_lock)
                {
                    return _clients.Keys.ToList();
                }
            }
        }
        public void RemoveClient(string id)
        {
            lock(_client_lock)
            {
                if (_clients.ContainsKey(id))
                {
                    _clients.Remove(id);
                }
                if (_nodes.ContainsKey(id))
                {
                    _nodes.Remove(id);
                }
                Logger.Log("RemoveClient", "detached node " + id, Logger.LogLevel.WARN);
            }
        }
        public int Count
        {
            get 
            {
                lock(_client_lock)
                {
                    return _clients.Count;
                }
            }
        }

        public void ResetHeartbeat(string node)
        {
            lock(_client_lock)
            {
                if (!_heartbeats.ContainsKey(node))
                    _heartbeats.Add(node, 0);
                _heartbeats[node] = 0;
            }
        }
        public void IncrementHeartbeats()
        {
            lock(_client_lock)
            {
                foreach(string node in _heartbeats.Keys)
                    _heartbeats[node] = _heartbeats[node]+1;
            }
        }
        public bool IsTimedOut(string node)
        {
            lock(_client_lock)
            {
                if (!_heartbeats.ContainsKey(node))
                    _heartbeats.Add(node, 0);
                return _heartbeats[node] > Params.TIMEOUT_LIMIT;
            }
        }
        
        // SINGLETON
        private static readonly object _lock = new object();
        private static Cluster _instance = null;
        public static Cluster Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if(_instance==null)_instance=new Cluster();
                    return _instance;
                }
            }
        }
    }
}