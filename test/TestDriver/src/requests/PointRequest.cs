using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace TangibleDriver 
{
    public class PointRequest
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public ActionType Type {get; set;}
        public string ID {get; set;}
        public List<string> PointIDs {get; set;}
        public string Value {get; set;}
        // public string ReturnTopic {get; set;}

        // DEBUGGING
        public string T0 {get; set;} 
        public string T1 {get; set;}
        public string T2 {get; set;}
        public string T3 {get; set;}
    }
}