using System.Collections.Generic;
using System.Linq;

namespace Node 
{
    class RequestQueue
    {
        // requests that are not yet taken up
        private LinkedList<Request> _queue = new LinkedList<Request>();

        // id, return topic [all requests, removed when completed]
        private Dictionary<string, Request> _current_requests = new Dictionary<string, Request>();
        private HashSet<string> _completed_requests = new HashSet<string>();
        private static readonly object _lock = new object();
        private static RequestQueue _instance = null;

        public static RequestQueue Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance==null)_instance=new RequestQueue();
                    return _instance;
                }
            }
        }

        public string GetReturnTopic(string id)
        {
            lock(_lock)
            {
                return _current_requests[id].ReturnTopic;
            }
        }

        public void Enqueue(Request request)
        {
            lock(_lock)
            {
                if(!_queue.Any(r => r.ID == request.ID))
                {
                    _queue.AddLast(request);
                    _current_requests.Add(request.ID, request);
                    Logger.Log("RequestQueue", "["+_queue.Count+"] Queued new request " + request.ID, Logger.LogLevel.INFO);
                }
            }
        }

        public void CompleteRequest(string id)
        {
            lock(_lock)
            {
                if(!_completed_requests.Contains(id))
                    _completed_requests.Add(id);
            }
        }

        public void DetachRequest(string id)
        {
            lock(_lock)
            {
                if (_current_requests.ContainsKey(id))
                    _current_requests.Remove(id);
                
                if (_completed_requests.Contains(id))
                    _completed_requests.Remove(id);
            }
        }

        public List<string> CompletedRequests
        {
            get 
            {
                lock(_lock)
                {
                    return _completed_requests.ToList();    
                }
            }   
        }

        public Request Peek(int index)
        {
            lock(_lock)
            {
                if (_queue.Count < 1) return null;
                if (index == 0)
                    return _queue.First.Value; 
                else if(_queue.Count > index)
                {
                    return _queue.ElementAt(index);
                } else return null;
            }
        }

        public void Dequeue()
        {
            lock(_lock)
            {
                _queue.RemoveFirst();
            }
        }

        ///<summary>
        ///<para>returns a list of strings indicating queued requests</para>
        ///<summary>
        public List<string> QueuedRequests
        {
            get 
            {
                lock(_lock)
                {
                    return _current_requests.Keys.ToList();
                }
            }
        }

        public List<Request> _QueuedRequests
        {
            get 
            {
                lock(_lock)
                {
                    return _current_requests.Values.ToList();
                }
            }
        }

        public int Count 
        {
            get 
            {
                lock(_lock)
                {
                    return _current_requests.Count;
                }
            }
        }

        public int CountUnfinished
        {
            get
            {
                lock(_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public bool ContainsRequest(string id)
        {
            lock(_lock)
            {
                return _current_requests.ContainsKey(id);
            }
        }
    }
}