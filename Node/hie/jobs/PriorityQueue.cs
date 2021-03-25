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

        public void SetRunAs(string sd)
        {
            lock(_lock)
            {
                foreach(Job j0 in _Queue)
                {
                    if (j0.ID == sd)
                    {
                        if(j0.TypeOf == Job.Type.SD)
                        {
                            j0.TypeOf = Job.Type.OP;
                        } else j0.TypeOf = Job.Type.SD;

                        break;
                    }
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
                    
                    if (job.TypeOfRequest == RequestType.SUBSCRIBE && job.TypeOf == Job.Type.SD)
                    {
                        Ledger.Instance.AddToAllParts(job.CounterPart, job.ID);
                        Ledger.Instance.AddToMyParts(job.CounterPart, job.ID);
                    } else if (job.TypeOfRequest == RequestType.SUBSCRIBE && job.TypeOf == Job.Type.OP)
                    {
                        Ledger.Instance.AddToAllParts(job.ID, job.CounterPart);
                    }

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