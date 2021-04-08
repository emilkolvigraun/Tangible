using System.Collections.Generic;

namespace TangibleDriver 
{
    public class ValueResponse 
    {
        public string ActionID {get; set;}
        // Point (value, time)
        public Dictionary<string, (string Value, string Time)> Message {get; set;}
        public string Timestamp {get; set;}
        // public string ReturnTopic {get; set;}
        public string T0123 {get; set;} 
    }
}