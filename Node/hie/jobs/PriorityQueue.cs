using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class PriorityQueue
    {
        private readonly object _lock = new object();
        private List<Job> _Queue = new List<Job>();

        public bool UpdateCounterPart(string jobId, string newNode)
        {
            lock(_lock)
            {
                Console.WriteLine(_Queue.Count);
                foreach(Job j1 in _Queue)
                {
                    if (j1.ID == jobId)
                    {
                        j1.CounterPart = (newNode, j1.CounterPart.JobId);
                        return true;
                    } 
                }
                return false;
            }
        }

        public List<Job> Queue 
        {
            get 
            {
                lock(_lock)
                {
                    return _Queue;
                }
            }
        }

        public void EnqueueJob(Job job)
        {
            lock(_lock)
            {
                try 
                {
                    if (_Queue.Any(j => j.ID == job.ID)) return;
                    int index = -1;
                    if (_Queue.Count > 0) index = LastIndexOf(job);

                    _Queue.Insert(index+1, job);
                    Logger.Log("PriorityQueue", "Added " + job.TypeOfRequest + " to priority queue", Logger.LogLevel.INFO);
                    
                } catch (Exception e)
                {
                    Logger.Log("PriorityQueue", e.Message, Logger.LogLevel.ERROR);
                }
            }
        }

        private int LastIndexOf(Job job)
        {
            int index = -1;
            int priority = job.Priority;
            while (index == -1)
            {   
                index = _Queue.LastIndexOf(_Queue.Where(j => j.Priority == priority).LastOrDefault());
                priority++;
                if (priority >= 10) break;
            }
            return index;
        }

        public Job DequeueJob()
        {
            lock(_lock)
            {
                if (_Queue.Count == 0) return null;
                else {
                    Job job = _Queue.ElementAt(0);
                    _Queue.RemoveAt(0);
                    return job;
                }
            }
        }
    }
}