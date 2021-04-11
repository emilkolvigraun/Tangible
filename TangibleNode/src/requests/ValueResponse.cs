using System.Collections.Generic;

namespace TangibleNode 
{
    public class ValueResponse 
    {
        public string ActionID {get; set;}
        // Point (value, time)
        public Dictionary<string, (string Value, string Time)> Message {get; set;}
        public string ReturnTopic {get; set;}


        // DEBUGGING
        // public string Timestamp {get; set;}
        
        // public string T0123 {get; set;} 

        // public string T0 {get; set;}
        // public string T1 {get; set;}
        // public string T2 {get; set;}
    }
}