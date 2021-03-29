using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Driver
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        RESPONSE,
        EXECUTE,
        STATUS,
        EMPTY
    }
}