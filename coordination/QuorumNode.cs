using System.Collections.Generic;

namespace Node
{
    class QuorumNode
    {       
        public string CommonName { get; set; } = null;
        public string AdvertisedHostName {get; set;} = null;
        public int Port {get; set;} = -1;
        public float Workload {get; set;} = -1;
        public float HeartBeat {get; set;} = 0;
        public Dictionary<string, MetaNode> Quorum {get; set;} = null;
        public List<Task> Tasks {get; set;} = null;
    }
}