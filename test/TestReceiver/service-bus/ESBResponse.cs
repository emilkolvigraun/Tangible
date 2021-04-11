using System.Collections.Generic;

namespace TestReceiver
{
    class ESBResponse
    {
        public string Timestamp {get; set;}

        // Point (value, time)
        public Dictionary<string, (string Value, string Time)> Message {get; set;}

        // DEBUGGING
        public string ID {get; set;}
        public string Node {get; set;}
        
        // public string T01234 {get; set;} 
    }
}