using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class PriorityQueue
    { 
        private static readonly object _lock = new object();
        private static PriorityQueue _instance = null;
        public static PriorityQueue Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if(_instance==null)_instance=new PriorityQueue();
                    return _instance;
                }   
            }
        }
        private List<ActionRequest> _Queue = new List<ActionRequest>();
        public List<ActionRequest> Queue 
        {
            get 
            {
                lock(_lock)
                {
                    return _Queue.ToList();
                }
            }
        }
        

        // follower uses this to maintain a copy of the priority queue
        public void DetachRequest(HashSet<string> ids)
        {
            lock(_lock)
            {
                _Queue.RemoveAll(r => ids.Contains(r.ID));
            }
        }

        public HashSet<string> ActionRequestIds
        {
            get 
            {
                lock(_lock)
                {
                    HashSet<string> ids = new HashSet<string>();
                    _Queue.ForEach(a => ids.Add(a.ID));
                    return ids;
                }
            }
        }

        // leader uses this to compare
        public List<ActionRequest> GetQueueAsList
        {
            get 
            {
                lock(_lock)
                {
                    return _Queue.ToList();
                }
            }
        }





        public void Enqueue(ActionRequest request)
        {
            lock(_lock)
            {
                try 
                {
                    int index = -1;
                    // if(_Queue.Any(r => r.ID == request.ID)) return;
                    if (_Queue.Count > 0) index = LastIndexOf(request);

                    _Queue.Insert(index+1, request);

                    Logger.Log("PriorityQueue", "Added " + request.Action + " to priority queue", Logger.LogLevel.INFO);
                    
                } catch (Exception e)
                {
                    Logger.Log("PriorityQueue", e.Message, Logger.LogLevel.ERROR);
                }
            }
        }
        private int LastIndexOf(ActionRequest request)
        {
            int index = -1;
            int priority = request.Priority;
            while (index == -1)
            {   
                index = _Queue.LastIndexOf(_Queue.Where(j => j.Priority == priority).LastOrDefault());
                priority++;
                if (priority >= Params.MAX_PRIORITY) break;
            }
            return index;
        }
        public ActionRequest Dequeue()
        {
            lock(_lock)
            {
                if (_Queue.Count < 1) return null;
                else {
                    ActionRequest job = _Queue.ElementAt(0);
                    _Queue.RemoveAt(0);
                    return job;
                }
            }
        }

        public int Count 
        {
            get 
            {
                lock(_lock)
                {
                    return _Queue.Count;
                }
            }
        }
    }
}