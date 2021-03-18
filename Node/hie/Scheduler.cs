using System.Collections.Generic;

namespace Node 
{
    class Scheduler 
    {
        private Dictionary<string, Job> RunningJobs {get;} = new Dictionary<string, Job>();
        private PriorityQueue JobsNotStarted {get;} = new PriorityQueue();


        public void QueueJobs(Job[] jobs)
        {
            lock(_not_started_lock)
            {
                foreach(Job job in jobs)
                    QueueJob(job);
            }
        }
        public void QueueJob(Job job)
        {
            lock(_not_started_lock)
            {
                JobsNotStarted.EnqueueJob(job);
            }
        }

        public Job[] Jobs
        {
            get 
            {
                List<Job> Jobs = new List<Job>();
                lock (_not_started_lock)
                {
                    Jobs.AddRange(JobsNotStarted.Queue);
                }
                lock (_running_jobs_lock)
                {
                    Jobs.AddRange(RunningJobs.Values);
                }
                return Jobs.ToArray();
            }
        }


        private static Scheduler _instance = null;
        private static readonly object _lock = new object();
        private readonly object _not_started_lock = new object();
        private readonly object _running_jobs_lock = new object();

        public static Scheduler Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance == null) _instance = new Scheduler();
                    return _instance;
                }
            }
        }
    }
}