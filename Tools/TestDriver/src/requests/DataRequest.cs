using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace TangibleDriver 
{
    public class DataRequest
    {
        public enum _Type
        {
            READ, WRITE, SUBSCRIBE, SUBSCRIBE_STOP, SUBSCRIBE_START
        }

        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public Dictionary<string, List<string>> PointDetails {get; set;}
        public string Image {get; set;}
        public string Value {get; set;}
        public int Priority {get; set;}
        public string ID {get; set;}
        public string Assigned {get; set;}
        public string ReturnTopic {get; set;}

        // DEBUGGING/EVALUATION
        public string Received  {get; set;}
    }
}