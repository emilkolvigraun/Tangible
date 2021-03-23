using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node 
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum DataType 
    {
        BENV,
    }
}