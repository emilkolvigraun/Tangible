using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Driver 
{
    class Execute : IRequest
    {
        // request ID
        public string ID {get; set;}

        // sensor ID [the sensor to execute the request on]
        public string PointID {get; set;}

        // for associating the executive with the correct job
        public string JobID {get; set;}

        // is it a read, write or subscribe?
        [JsonConverter(typeof(StringEnumConverter))] 
        public ActionType TypeOfAction {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public RequestType TypeOf {get; set;}

        // If it is operational or shadow
        [JsonConverter(typeof(StringEnumConverter))] 
        public JobType JobType {get; set;}

        // if it is a write, then we obviously need a value to write
        public string Value {get; set;} = null;
    }
}