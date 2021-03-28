using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node 
{
    [JsonConverter(typeof(StringEnumConverter))] 
    enum ActionType 
    {
        READ,
        WRITE,
        SUBSCRIBE
    }
}