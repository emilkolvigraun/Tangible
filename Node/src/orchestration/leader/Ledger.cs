using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class Ledger 
    {
        // LOG INFORMATION [used to schedule requests]
        private HashSet<string> _requestIDs = new HashSet<string>();

        // the information that this node contains about the other nodes
        private Dictionary<string, HashSet<Request>> _log = new Dictionary<string, HashSet<Request>>();

        // the requests that each client believes it has
        private Dictionary<string, HashSet<string>> _client_log = new Dictionary<string, HashSet<string>>();

        // the state of the priority queues in all other nodes
        private Dictionary<string, HashSet<string>> _client_priority_queues = new Dictionary<string, HashSet<string>>();

        // the information that this node believes the other nodes contain
        private Dictionary<string,  Dictionary<string, HashSet<string>>> _cluster_state = new Dictionary<string,  Dictionary<string, HashSet<string>>>();
        private readonly object _log_lock = new object();

        // overall adding and removing clients from fov
        public void InitClient(string id)
        {
            lock(_log_lock)
            {
                if (!_log.ContainsKey(id))
                    _log.Add(id, new HashSet<Request>());
                    
                if (!_cluster_state.ContainsKey(id))
                    _cluster_state.Add(id, new Dictionary<string, HashSet<string>>());

                if(!_client_log.ContainsKey(id))
                    _client_log.Add(id, new HashSet<string>());
            }
        }
        public void RemoveClient(string id)
        {
            lock(_log_lock)
            {
                if (_log.ContainsKey(id))
                    _log.Remove(id);
                
                if (_cluster_state.ContainsKey(id))
                    _cluster_state.Remove(id);
                
                if (_client_log.ContainsKey(id))
                    _client_log.Remove(id);

                if (_client_priority_queues.ContainsKey(id))
                    _client_priority_queues.Remove(id);
                
                foreach(KeyValuePair<string, Dictionary<string, HashSet<string>>> client in _cluster_state)
                {
                    if (client.Value.ContainsKey(id))
                        client.Value.Remove(id);
                }
            }
        }

        // These are all about maintaining the Log        
        public bool ContainsRequest(string id)
        {
            lock(_log_lock)
            {
                return _requestIDs.Contains(id);
            }
        }
        public void AddRequest(string node, Request request)
        {
            lock(_log_lock)
            {
                if (!ContainsRequest(request.ID))
                {
                    _requestIDs.Add(request.ID);
                    if (_log.ContainsKey(node))
                        _log[node].Add(request);
                    else _log.Add(node, new HashSet<Request>{request});
                }
            }
        }
        public void RemoveRequest(string node, Request request)
        {
            lock(_log_lock)
            {
                if (_requestIDs.Contains(request.ID))
                {
                    _log[node].RemoveWhere(r => r.ID == request.ID);
                    _requestIDs.Remove(request.ID);
                }
            }
        }
        public void RemoveRequest(string node, string requestID)
        {
            lock(_log_lock)
            {
                if (_requestIDs.Contains(requestID))
                {
                    _log[node].RemoveWhere(r => r.ID == requestID);
                    _requestIDs.Remove(requestID);
                }
            }
        }


        // Utility to get the information in a simple format
        public HashSet<string> RequestIDs
        {
            get 
            {
                lock(_log_lock)
                {
                    return _requestIDs;
                }
            }
        }
        public bool RequestIDsContains(string id)
        {
            lock(_log_lock)
            {
                return _requestIDs.Contains(id);
            }
        }
        public int RequestCount(string id)
        {
            lock(_log_lock)
            {
                if (!_log.ContainsKey(id)) return 0;
                return _log[id].Count;
            }
        }

        public void RescheduleRequests(string id)
        {
            lock (_log_lock)
            {
                if (_log.ContainsKey(id))
                {                    
                    ScheduleRequests(_log[id].ToList(), id);
                }
            }
        }

        private HardwareAbstraction hal = new HardwareAbstraction();
        ///<summary>
        ///<para>appends the request to the node with least jobs</para>
        ///</summary>
        public void ScheduleRequest(ActionRequest request, string id = "")
        {
            lock(_log_lock)
            {
                try 
                {
                    List<Request> rqs = hal.CreateRequests(request);
                    ScheduleRequests(rqs);
                    
                } catch (Exception e)
                {
                    Logger.Log("ScheduleRequest", "Error: " + e.Message, Logger.LogLevel.ERROR);
                }
            }
        }

        public void ScheduleRequests(List<Request> requests, string id = "")
        {
            lock(_log_lock)
            {
                requests.ForEach(r => {
                    if (_log.Count < 1 && Cluster.Instance.Count < 1)
                    {
                        RequestQueue.Instance.Enqueue(r);
                    } else 
                    {
                        string node = _log.Aggregate((l, r) => l.Value.Count < r.Value.Count && l.Key != id ? l : r).Key;
                        AddRequest(node, r);
                        Logger.Log("ScheduleRequest", "Scheduled request to " + Cluster.Instance.GetNode(node).Name, Logger.LogLevel.INFO);
                    }  
                });
            }
        }

        // For maintaining the cluster state
        public void UpdateClientState(string node, Dictionary<string, List<string>> LogDetach, Dictionary<string, List<Request>> LogAppend, 
            List<Request> Enqueue, List<string> NodeDetach, List<Node> NodeAttach, List<string> Dequeue, List<string> Completed,
            HashSet<string> PriorityDetach, List<ActionRequest> PriorityAttach)
        {
            lock(_log_lock)
            {

                if (!_cluster_state.ContainsKey(node))
                    _cluster_state.Add(node, new Dictionary<string, HashSet<string>>());

                // node attach
                foreach(Node client in NodeAttach)
                {
                    _cluster_state[node].Add(client.Key, new HashSet<string>());
                }

                // append
                foreach(KeyValuePair<string, List<Request>> append in LogAppend)
                {
                    if (!_cluster_state[node].ContainsKey(append.Key))
                        _cluster_state[node].Add(append.Key, new HashSet<string>());

                    foreach(Request r0 in append.Value)
                        _cluster_state[node][append.Key].Add(r0.ID);
                }
            
                // detach
                foreach(KeyValuePair<string, List<string>> detach in LogDetach)
                {
                    if (!_cluster_state[node].ContainsKey(detach.Key))
                        _cluster_state[node].Add(detach.Key, new HashSet<string>());

                    foreach(string r0 in detach.Value)
                        _cluster_state[node][detach.Key].Remove(r0);
                }
            
                // enqueue request
                if(!_client_log.ContainsKey(node))
                    _client_log.Add(node, new HashSet<string>());
                foreach(Request r0 in Enqueue)
                {
                    _client_log[node].Add(r0.ID);
                }

                // dequeue request
                foreach(string r0 in Dequeue)
                {
                    if (_client_log[node].Contains(r0))
                        _client_log[node].Remove(r0);
                }
            
                // node detach
                foreach(string n0 in NodeDetach)
                {
                    if (_cluster_state[node].ContainsKey(n0))
                        _cluster_state[node].Remove(n0);
                }

                if (!_log.ContainsKey(node))
                    _log.Add(node, new HashSet<Request>());
                
                HashSet<string> _completed = new HashSet<string>(Completed);
                _log[node].RemoveWhere(r => _completed.Contains(r.ID));

                foreach(ActionRequest acr in PriorityAttach)
                {
                    _client_priority_queues[node].Add(acr.ID);
                }

                _client_priority_queues[node].RemoveWhere(r => PriorityDetach.Contains(r));
                
            }
        }
        public (Dictionary<string, List<string>> LogDetach, Dictionary<string, List<Request>> LogAppend, List<Request> Enqueue, List<string> NodeDetach, List<Node> NodeAppend, List<string> Dequeue, HashSet<string> PriorityDetach, List<ActionRequest> PriorityAttach) GetClientUpdates(string node)
        {
            
            lock(_log_lock)
            {
                Dictionary<string, List<Request>> append = new Dictionary<string, List<Request>>();
                List<Request> enqueue = new List<Request>();
                Dictionary<string, List<string>> detach = new Dictionary<string, List<string>>();
                List<string> nodeDetach = new List<string>();
                List<Node> nodeAppend = new List<Node>();
                List<string> dequeue = new List<string>();
                HashSet<string> priorityDetach = new HashSet<string>();
                List<ActionRequest> priorityAttach = new List<ActionRequest>();

                Dictionary<string, HashSet<string>> currentClient;
                if (!_cluster_state.ContainsKey(node))
                    _cluster_state.Add(node, new Dictionary<string, HashSet<string>>());
                currentClient = _cluster_state[node];

                try 
                {
                    // enqueue
                    if (!_client_log.ContainsKey(node))
                        _client_log.Add(node, new HashSet<string>());
                    HashSet<string> currentRequests = _client_log[node];

                    // append
                    foreach (KeyValuePair<string, HashSet<Request>> client in _log)
                    {
                        if (client.Key != node && !currentClient.ContainsKey(client.Key) && !(nodeAppend.Count > Params.BATCH_SIZE))
                        {
                            nodeAppend.Add(Cluster.Instance.GetNode(client.Key));
                        }
                        
                        try 
                        {
                            // enqueue
                            // skip if it is the same client
                            if (client.Key == node)
                            {
                                foreach (Request r0 in _log[node])
                                {
                                    if (!currentRequests.Contains(r0.ID) && !(enqueue.Count > Params.BATCH_SIZE))
                                        enqueue.Add(r0);
                                }
                                continue;                       
                            }
                        } catch (Exception e)
                        {
                            Logger.Log("ClientUpdates", "Enqueue, " + e.Message, Logger.LogLevel.ERROR);
                        }

                        if (client.Key == node) continue;
                        // else, continue on ...
                        foreach(Request r0 in client.Value)
                        {
                            if (!currentClient.ContainsKey(client.Key) || (!append.ContainsKey(client.Key) && !currentClient[client.Key].Any(r => r == r0.ID)))
                                append.Add(client.Key, new List<Request>());
                            if (!currentClient.ContainsKey(client.Key) || !currentClient[client.Key].Any(r => r == r0.ID))
                                if (!append[client.Key].Contains(r0))
                                    append[client.Key].Add(r0);

                            if (append.ContainsKey(client.Key) && append[client.Key].Count > Params.BATCH_SIZE) break;
                        }
                        if (append.Values.Count > Params.BATCH_SIZE) break;
                    }
                } catch (Exception e)
                {
                    Logger.Log("RequestAppend", e.Message, Logger.LogLevel.ERROR);
                }
                
                try 
                {
                    if (!currentClient.ContainsKey(Params.UNIQUE_KEY))
                        nodeAppend.Add(new Node(){
                            Host = Params.ADVERTISED_HOST_NAME,
                            Name = Params.NODE_NAME,
                            Port = Params.PORT_NUMBER,
                            Key = Params.UNIQUE_KEY
                        });
                } catch (Exception e)
                {
                    Logger.Log("AppendSelf", "[0] " + e.Message, Logger.LogLevel.ERROR);
                }

                try 
                {
                    if (!_client_priority_queues.ContainsKey(node))
                        _client_priority_queues.Add(node, new HashSet<string>());


                    foreach(ActionRequest acr in PriorityQueue.Instance.GetQueueAsList)
                    {
                        if (!_client_priority_queues[node].Contains(acr.ID))
                            priorityAttach.Add(acr);
                        if(priorityAttach.Count > Params.BATCH_SIZE) break;
                    }
                } catch (Exception e)
                {
                    Logger.Log("PriorityQueue", "Attach, " + e.Message, Logger.LogLevel.ERROR);
                }
                try 
                {
                    if (!_client_priority_queues.ContainsKey(node))
                        _client_priority_queues.Add(node, new HashSet<string>());

                    HashSet<string> priorityQueueRequestIds = PriorityQueue.Instance.ActionRequestIds;
                    foreach(string id in _client_priority_queues[node])
                    {
                        if (!priorityQueueRequestIds.Contains(id))
                            priorityDetach.Add(id);
                        if(priorityDetach.Count > Params.BATCH_SIZE) break;
                    }
                } catch (Exception e)
                {
                    Logger.Log("PriorityQueue", "Detach, " + e.Message, Logger.LogLevel.ERROR);
                }

                try 
                {
                    if (currentClient.ContainsKey(Params.UNIQUE_KEY)) foreach(Request r0 in RequestQueue.Instance._QueuedRequests)
                    {                        
                        if (append.Values.Count > Params.BATCH_SIZE) break;

                        if (!append.ContainsKey(Params.UNIQUE_KEY) && !currentClient[Params.UNIQUE_KEY].Any(r => r == r0.ID))
                                append.Add(Params.UNIQUE_KEY, new List<Request>());
                        if (!currentClient[Params.UNIQUE_KEY].Any(r => r == r0.ID))
                            if (!append[Params.UNIQUE_KEY].Contains(r0))
                                append[Params.UNIQUE_KEY].Add(r0);

                        if (append.ContainsKey(Params.UNIQUE_KEY) && append[Params.UNIQUE_KEY].Count > Params.BATCH_SIZE) break;
                    }
                } catch(Exception e)
                {
                    Logger.Log("AppendSelf", "[1] " + e.Message, Logger.LogLevel.ERROR);
                }

                try 
                {
                    // request detach + node detach
                    foreach (KeyValuePair<string, HashSet<string>> client in currentClient)
                    {
                        if (!_log.ContainsKey(client.Key) && client.Key != Params.UNIQUE_KEY)
                        {
                            nodeDetach.Add(client.Key);
                            continue;
                        }

                        if (client.Key == Params.UNIQUE_KEY)
                        {
                            foreach(string r0 in client.Value)
                            {
                                if (!RequestQueue.Instance.ContainsRequest(r0))
                                {
                                    if (!detach.ContainsKey(client.Key))
                                        detach.Add(client.Key, new List<string>());
                                    detach[client.Key].Add(r0);
                                }

                                if(detach.Values.Count > Params.BATCH_SIZE) break;
                            }
                        } else 
                        {
                            foreach(string r0 in client.Value)
                            {
                                if (!_log[client.Key].Any(r => r.ID == r0))
                                {
                                    if (!detach.ContainsKey(client.Key))
                                        detach.Add(client.Key, new List<string>());
                                    detach[client.Key].Add(r0);
                                }

                                if(detach.Values.Count > Params.BATCH_SIZE) break;
                            }
                        }
                        
                        if(detach.Values.Count > Params.BATCH_SIZE) break;
                    }
                } catch (Exception e)
                {
                    Logger.Log("ClientUpdates", "Detach, " + e.Message, Logger.LogLevel.ERROR);
                }
                
                try 
                {
                    // dequeue
                    HashSet<string> currentRequests = _client_log[node];
                    foreach (string r0 in currentRequests)
                    {
                        if (!_log[node].Any(r => r.ID == r0))
                            dequeue.Add(r0);

                        if(dequeue.Count > Params.BATCH_SIZE) break;
                    }
                } catch (Exception e)
                {
                    Logger.Log("ClientUpdates", "Dequeue, " + e.Message, Logger.LogLevel.ERROR);
                }

                return (detach, append, enqueue, nodeDetach, nodeAppend, dequeue, priorityDetach, priorityAttach);
            }
        }

        public Dictionary<string, HashSet<string>> GetLog
        {
            get 
            {
                lock(_log_lock)
                {
                    Dictionary<string, HashSet<string>> log = new Dictionary<string, HashSet<string>>();
                    foreach (KeyValuePair<string, HashSet<Request>> entry in _log)
                    {
                        HashSet<string> h = new HashSet<string>();
                        foreach(Request r0 in entry.Value)
                        {
                            h.Add(r0.ID);
                        }
                        log.Add(entry.Key, h);
                    }
                    return log;
                }
            }
        }

        /// SINGLETON
        private static readonly object _instance_lock = new object();
        private static Ledger _instance = null;

        public static Ledger Instance 
        {
            get 
            {
                lock(_instance_lock)
                {
                    if (_instance==null)_instance=new Ledger();
                    return _instance;
                }
            }
        }
    }
}