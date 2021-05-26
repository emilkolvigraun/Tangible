using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleNode 
{
    public class Call
    {

        public enum _Type 
        {
            DATA_REQUEST,
            NODE_ADD,
            NODE_DEL,
            VOTE,
            VALUE_RESPONSE
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public byte[] Data {get; set;}
    }
}