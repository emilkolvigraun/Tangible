using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum RequestType 
    {
        HI, // hie request [job append]
        AE, // Append entries request
        AR, // Append entries response
        BC, // Broadcast
        RS, // register
        CT, // certificate
        VT, // voting
        ST, // status response
        EMPTY, // empty
        NIL, // no expectation
        RN, // run as operative

        // ACTION TYPES
        SUBSCRIBE,
        READ,
        WRITE,
        CREATE_USER,
        CREATE_ROLE
    }
}