using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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


        // [JsonConverter(typeof(StringEnumConverter))] 
        public List<Logger.Tag> LogLevel {get; set;} // = new List<Logger.Tag>{Logger.Tag.DEBUG, Logger.Tag.COMMIT, Logger.Tag.INFO, Logger.Tag.WARN, Logger.Tag.ERROR, Logger.Tag.FATAL};


        public List<Credentials> Members {get; set;} = new List<Credentials>();

        // DEBUGGING
        public Test Testing {get; set;} = new Test();
        public Optional Optional {get; set;} = new Optional();
    }
}