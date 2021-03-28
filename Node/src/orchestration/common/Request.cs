using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node 
{
    class Request
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public ActionType TypeOf {get; set;}
        public string ID {get; set;}
        public string Image {get; set;}
        public string PointID {get; set;}
        public string ReturnTopic {get; set;}
        public string Value {get; set;} = null;
        public long Timestamp {get; set;}
    }
}