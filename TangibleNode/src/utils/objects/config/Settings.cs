using System.Collections.Generic;

namespace TangibleNode
{
    class Settings 
    {
        public string Host {get; set;} = string.Empty;
        public int Port {get; set;} = -1;
        public string ID {get; set;} = string.Empty;
        public string RDFPath {get; set;} = string.Empty;

        public string BroadcastTopic {get; set;} = string.Empty;
        public string RequestTopic {get; set;} = string.Empty;

        public string DockerRemoteHost {get; set;} = string.Empty;
        public string DriverHostName {get; set;} = string.Empty;
        public int DriverPortRangeStart {get; set;} = 8000;
        public int DriverPortRangeEnd {get; set;} = 8100;

        public Optional Optional {get; set;} = default(Optional);

        public List<Sender> Members {get; set;} = new List<Sender>();

        // DEBUGGING
        public Test Testing {get; set;} = new Test();
    }
}