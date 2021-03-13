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
        ST, // status response
        EMPTY, // empty
        NIL // no expectation
    }
}