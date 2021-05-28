using System.Collections.Generic;

namespace TangibleNode
{
    class Broadcast 
    {
        public Credentials Self {get; set;} = Credentials.Self;
        public List<Credentials> Members {get; set;} = StateLog.Instance.Nodes.AsCredentials;
    }
}