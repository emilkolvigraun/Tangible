using System.Collections.Generic;

namespace Node 
{
    class Container 
    {

        // Making sure that the container is thread-safe
        private readonly object _lock = new object();

        // The ID is assigned from docker
        public string ID {get; set;} = null;

        // The Image is assigned when the container instance is created
        public string Image {get; set;} = null;

        // Jobs are added and removed based on their status
        public Dictionary<string, Job> Deployed {get;} = new Dictionary<string, Job>();

        public Dictionary<string, Job> Jobs 
        {
            get 
            {
                lock (_lock)
                {
                    return Deployed;
                }
            }
        }

        public bool Append(Job job)
        {
            lock (_lock)
            {
                if (!Deployed.ContainsKey(job.ID))
                {
                    Deployed.Add(job.ID, job);
                    Logger.Log("Deploy", "Assigned job to container [job:"+job.ID+", image:"+Image+"]", Logger.LogLevel.IMPOR);
                    return false;
                } else {
                    Logger.Log("Deploy", "Unable to deploy job: A similar job already exists", Logger.LogLevel.ERROR);
                }
                return true;
            }
        }

    }
}