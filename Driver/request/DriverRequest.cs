using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace Driver 
{
    class DriverRequest : IRequest
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public RequestType TypeOf {get; set;} = RequestType.EXECUTE;
        public string ID {get; set;}
        [JsonConverter(typeof(StringEnumConverter))] 
        public ActionType Action {get; set;}
        public string Value {get; set;}
        public string PointID {get; set;}
        public long T0 {get; set;}
    }
}