using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        AE, // Append entries
        BC, // Broadcast
        RS, // register
        CT, // certificate
        VT, // voting
        CN, // connect with nodes
        EMPTY, // empty
        NIL // no expectation
    }
}