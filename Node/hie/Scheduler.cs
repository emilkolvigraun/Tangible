using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    class Scheduler 
    {
        private PriorityQueue JobsNotStarted {get;} = new PriorityQueue();
        private Dictionary<string, Driver> DeployedDrivers {get;} = new Dictionary<string, Driver>();
        public Job[] _Jobs 
        {
            get 
            {
                List<Job> Jobs = new List<Job>();
                lock (_not_started_lock)  
                {
                    Jobs.AddRange(JobsNotStarted.Queue);
                }
                lock(_containers_lock) 
                {
                    foreach (KeyValuePair<string, Driver> c in DeployedDrivers)
                    {
                        Jobs.AddRange(c.Value.Jobs.Values);
                    }
                }
                return Jobs.ToArray();
            }
        }
        public int NumberOfJobs
        {
            get 
            {
                lock (_not_started_lock) lock (_containers_lock)
                {
                    return _Jobs.Length;
                }
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

        public void TransmitRunAs(string sd)
        {
            lock(_not_started_lock)
            {
                JobsNotStarted.SetRunAs(sd);
            }

            lock(_containers_lock)
            {
                foreach(Driver d in DeployedDrivers.Values)
                {
                    if(d.SetRunAs(sd)) break;
                }
            }
        }
        
        public string GetScheduledNode(Job job, string ignore = "")
        {
            // if (ignore != "") Logger.Log("ScheduleJob", "Ignoring " + ignore, Logger.LogLevel.INFO);
            Dictionary<string, MetaNode> cluster = Ledger.Instance.ClusterCopy;
            string node = null;
            if (cluster.Count > 1)
            {
                MetaNode n0 = cluster.ElementAt(0).Value;
                foreach (MetaNode nN in cluster.Values)
                {
                    if (nN.Name != ignore && nN.Name != n0.Name && nN.Jobs.Length < n0.Jobs.Length 
                        && nN.Jobs.Length <= AdjustedNumberOfJobs)
                    {
                        node = nN.Name;
                    }
                }
                if (node == null && ignore != n0.Name && n0.Jobs.Length <= AdjustedNumberOfJobs)
                {
                    node = n0.Name;
                } else if (node == null) node = Params.NODE_NAME;
            } else if (cluster.Count == 1)
            {
                MetaNode n0 = cluster.ElementAt(0).Value;
                if (ignore != n0.Name && n0.Jobs.Length <= AdjustedNumberOfJobs)
                {
                    node = n0.Name;
                } else node = Params.NODE_NAME;
            } else node = Params.NODE_NAME;

            return node;
        }
        
        public string ScheduleJob(Job job, string ignore = "")
        {
            string node = GetScheduledNode(job, ignore);
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
                if(!status)
                { 
                    Logger.Log("ScheduleJob", "Failed to schedule job.... Retrying", Logger.LogLevel.WARN);
                    return ScheduleJob(job, ignore);
                } else {
                    Logger.Log("ScheduleJob", "Scheduled job " + job.ID + " as " + job.TypeOf.ToString(), Logger.LogLevel.INFO);
                }
            }

            return node;
        }

        public bool ScheduleFinishedJob(string node, Job job)
        {
            bool status = false;
            if (node == Params.NODE_NAME)
            {
                lock(_not_started_lock)
                {
                    // add the job to thyself
                    JobsNotStarted.EnqueueJob(job);
                    status = true;
                }
            } else 
            {
                // else, add the job to the follower node
                status = Ledger.Instance.AddJob(node, job);
            }
            return status;
        }
        
        private int AdjustedNumberOfJobs
        {
            get 
            {
                lock (_not_started_lock) lock(_containers_lock)
                {
                    return ((int)(_Jobs.Length+1)*2);
                }
            }
        }
        public void NextJob()
        {
            try 
            {
                lock (_containers_lock)
                {
                    foreach(Driver d in DeployedDrivers.Values)
                    {
                        if (!d.IsStarted & !d.IsRunning)
                        {
                            d.StartDriver();
                        }     
                        
                        if (!d.IsVerifyingState)
                        {
                            d.VerifyState();
                        }

                        if (d.IsRunning && !d.IsStarted && !d.IsTransmitting)
                        {
                            d.TransmitNewJobs();
                        }
                    }
                }

                Job nextJob = JobsNotStarted.DequeueJob();
                if (nextJob != null) 
                {
                    lock(_containers_lock)
                    {
                        bool error = GetOrCreateContainer(nextJob.Image).AppendJob(nextJob);
                        if (error)
                        {
                            Logger.Log("NextJob", "There was a problem deploying the job...", Logger.LogLevel.ERROR);
                        }
                    }
                }
            } catch(Exception e)
            {
                Logger.Log("NextJob", "[0] " + e.Message, Logger.LogLevel.ERROR);
            }
        }
        private Driver GetOrCreateContainer(string image)
        {
            lock(_containers_lock)
            {
                if (!DeployedDrivers.ContainsKey(image))
                {
                    string machineName = Utils.GetUniqueKey(size:10);
                    int port = Params.UNUSED_PORT;
                    DeployedDrivers.Add(image, new Driver(){
                        Host = Params.HIE_ADVERTISED_HOST_NAME,
                        MachineName = machineName,
                        Image = image,
                        Port = port
                    });
                }
                return DeployedDrivers[image];
            }
        }
        private static Scheduler _instance = null;
        private static readonly object _lock = new object();
        private readonly object _containers_lock = new object();
        private readonly object _not_started_lock = new object();
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