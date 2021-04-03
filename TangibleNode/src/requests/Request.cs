using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleNode 
{
    public class Request
    {

        public enum _Type 
        {
            ACTION,
            NODE_ADD,
            NODE_DEL,
            VOTE,
            COMPLETE,
            DRIVER_RESPONSE
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public byte[] Data {get; set;}
    }
}