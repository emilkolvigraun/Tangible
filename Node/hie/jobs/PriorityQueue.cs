using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class PriorityQueue
    {
        private readonly object _lock = new object();
        private List<Job> _Queue = new List<Job>();

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
                    int index = -1;
                    if (_Queue.Count > 0) index = LastIndexOf(job);
                    if (!_Queue.Any(j => j.ID == job.ID)) 
                    {
                        _Queue.Insert(index+1, job);
                        Logger.Log("PriorityQueue", "Added " + job.Request.TypeOf + " to priority queue", Logger.LogLevel.INFO);
                    }
                } catch (Exception e)
                {
                    Logger.Log("PriorityQueue", e.Message, Logger.LogLevel.ERROR);
                }
            }
        }

        private int LastIndexOf(Job job)
        {
            int index = -1;
            int priority = job.Request.Priority;
            while (index == -1)
            {   
                index = _Queue.LastIndexOf(_Queue.Where(j => j.Request.Priority == priority).LastOrDefault());
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