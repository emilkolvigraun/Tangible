using System.Collections.Generic;

namespace Node
{
    class Description
    {       
        public string CommonName { get; set; } = null;
        public string AdvertisedHostName {get; set;} = null;
        public int Port {get; set;} = -1;
        public float Workload {get; set;} = -1;
        public float HeartBeat {get; set;} = 0;
        public Dictionary<string, QuorumNode> Quorum {get; set;} = null;
        public List<Task> Tasks {get; set;} = null;
    }
}