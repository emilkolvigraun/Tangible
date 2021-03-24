using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node 
{
    class RunAsRequest : IRequest
    {
        
        // request ID
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public RequestType TypeOf {get; set;} = RequestType.RN;
        public string JobID {get; set;}
    }
}