using System.Collections.Generic;

namespace TangibleNode
{
    class ESBResponse
    {
        // public string Timestamp {get; set;} = Utils.Micros.ToString();
        // Point (value, time)
        public Dictionary<string, (string Value, string Time)> Message {get; set;}
        // DEBUGGING
        public string ID {get; set;}
    }
}