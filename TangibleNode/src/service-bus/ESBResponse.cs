using System.Collections.Generic;

namespace TangibleNode
{
    class ESBResponse
    {
        // Response is always a json formatted as a string
        // public Dictionary<string, (string Value, string Time)> Message {get; set;}
        public ValueResponse Message {get; set;}
        public long Timestamp {get; set;}
        // DEBUGGING
        public string ID {get; set;}
    }
}