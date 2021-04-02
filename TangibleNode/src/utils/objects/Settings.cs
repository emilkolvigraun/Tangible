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

        public Optional Optional {get; set;} = default(Optional);

        public List<Node> TcpNodes {get; set;} = new List<Node>();
    }
}