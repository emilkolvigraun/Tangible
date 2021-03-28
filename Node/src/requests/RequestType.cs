using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        REGISTRATION,
        BROADCAST,
        APPEND_REQ,
        APPEND_RES,
        CERTIFICATE,
        VOTE,
        EXECUTE,
        STATUS,
        RESPONSE,
        EMPTY,

        // ACTION TYPES
        SUBSCRIBE,
        READ,
        WRITE
    }
}