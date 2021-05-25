using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleNode 
{
    class ESBDataRequest
    {

        [JsonConverter(typeof(StringEnumConverter))] 
        public DataRequest._Type Type {get; set;}
        public Location Benv {get; set;}
        public string Value {get; set;}
        public int Priority {get; set;}
        public string ReturnTopic {get; set;}
        public string Received {get; set;}
    }
}