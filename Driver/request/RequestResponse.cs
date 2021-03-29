using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace Driver 
{
    class RequestResponse : IRequest 
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public RequestType TypeOf {get; set;} = RequestType.RESPONSE;

        public string ID {get; set;}

        public long T0 {get; set;}
        public long T1 {get; set;}

        public string Value {get; set;}
    }
}