using System.Collections.Generic;
using System.Linq;

namespace Node 
{
    class Scheduler 
    {
        private Dictionary<string, Job> RunningJobs {get;} = new Dictionary<string, Job>();
        private PriorityQueue JobsNotStarted {get;} = new PriorityQueue();


        // public void QueueJobs(Job[] jobs)
        // {
        //     lock(_not_started_lock)
        //     {
        //         foreach(Job job in jobs)
        //             QueueJob(job);
        //     }
        // }
        // public void QueueJob(Job job)
        // {
        //     lock(_not_started_lock)
        //     {
        //         JobsNotStarted.EnqueueJob(job);
        //     }
        // }
        public Job[] _Jobs
        {
            get 
            {
                List<Job> Jobs = new List<Job>();
                lock (_not_started_lock) lock (_running_jobs_lock)
                {
                    Jobs.AddRange(JobsNotStarted.Queue);
                    Jobs.AddRange(RunningJobs.Values);
                }
                return Jobs.ToArray();
            }
        }

        public void UpdateJobs(Job[] jobs)
        {
            lock(_not_started_lock)
            {
                foreach (Job job in jobs)
                {
                    Logger.Log("AppendEntry", "Received job: [job:" + job.ID+"]", Logger.LogLevel.IMPOR);
                    JobsNotStarted.EnqueueJob(job);
                }
            }
        }

        public void ScheduleJob(Job job)
        {
            Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
            string node = null;
            if (cluster.Count > 1)
            {
                MetaNode n0 = cluster.ElementAt(0).Value;
                foreach (MetaNode nN in cluster.Values)
                {
                    if (nN.Name != n0.Name && nN.Jobs.Length < n0.Jobs.Length 
                        && nN.Jobs.Length <= _Jobs.Length)
                    {
                        node = nN.Name;
                    }
                }
                if (node == null && n0.Jobs.Length <= _Jobs.Length)
                {
                    node = n0.Name;
                } else if (node == null) node = Params.NODE_NAME;
            } else if (cluster.Count == 1)
            {
                MetaNode n0 = cluster.ElementAt(0).Value;
                if (n0.Jobs.Length <= _Jobs.Length)
                {
                    node = n0.Name;
                } else node = Params.NODE_NAME;
            } else node = Params.NODE_NAME;

            if (node == Params.NODE_NAME)
            {
                lock(_not_started_lock)
                {
                    // add the job to thyself
                    JobsNotStarted.EnqueueJob(job);
                }
            } else 
            {
                // else, add the job to the follower node
                bool status = Ledger.Instance.AddJob(node, job);

                // if the follower has stopped responding in the meantime
                // do all of this again
                if(!status) ScheduleJob(job);
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