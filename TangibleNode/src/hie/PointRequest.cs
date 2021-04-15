using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace TangibleNode 
{
    class PointRequest
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public Action._Type Type {get; set;}
        public string ID {get; set;}
        public List<string> PointIDs {get; set;}
        public string Value {get; set;}
        public string ReturnTopic {get; set;}
    }
}