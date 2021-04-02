using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Server
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        TEST_RESPONSE,
        STATUS,
        EMPTY
    }
}