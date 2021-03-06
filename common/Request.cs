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
        }

        public Type TypeOf {get; set;}
        public Dictionary<string, string> Data {get; set;} = null;
        public Description Node {get; set;}
    }
}