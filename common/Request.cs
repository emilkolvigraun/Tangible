using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Node
{ 
    class Request
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public enum Type
        {
            REGISTRATION, // Used when registering to a cluster
            CERTIFICATE, // Used to exchange certificate
            SHADOW, // Used to tell Node to produce a shadow clone of a Task
            MAINTAIN, // Used to tell a Node to execute and potentially maintain a Task
            HEARTBEAT, // Used when a node is pulsating
            LEADER_ELECTION, // When node becomes a candidate, it inits a votation round in the cluster
            MAJORITY_VOTE, // When a node is selected, it pushes the result to all other nodes,
            ACCEPTED, // When a command is excepted and the node overwrites the new with the old
            NOT_ACCEPTED // When a command is not excepted and the other node must overwrite
        }
        public Type TypeOf {get; set;}
        public DataObject Data {get; set;} = null;
        public Description Node {get; set;}
        public long TimeStamp {get; set;} = -1;
        public bool Overwrite {get; set;} = false;
    }
}