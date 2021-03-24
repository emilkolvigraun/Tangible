using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Driver 
{
    class RunAsRequest : IRequest
    {
                
        // request ID
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public RequestType TypeOf {get; set;}
        public string JobID {get; set;}
    }
}