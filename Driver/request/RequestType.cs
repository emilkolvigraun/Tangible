using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Driver
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        HI, // hie request [job append]
        RN, // run as operative
        ST, // status response
        RR, // request response
        EMPTY
    }
}