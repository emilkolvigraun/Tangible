using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Driver 
{
    public enum ActionType 
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        SUBSCRIBE,
        READ,
        WRITE
    }
}