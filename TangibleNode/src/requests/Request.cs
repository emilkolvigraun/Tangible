using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleNode 
{
    public class Request
    {

        public enum _Type 
        {
            DATA_REQUEST,
            NODE_ADD,
            NODE_DEL,
            VOTE,
            POINT
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public byte[] Data {get; set;}
    }
}