using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleNode 
{
    class DataRequest
    {

        [JsonConverter(typeof(StringEnumConverter))] 
        public Action._Type Type {get; set;}
        public Location Benv {get; set;}
        public string Value {get; set;}
        public int Priority {get; set;}
    }
}