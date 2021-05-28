using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleDriver 
{
    public class Call
    {

        public enum _Type 
        {
            VALUE_RESPONSE
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;} = _Type.VALUE_RESPONSE;
        public byte[] Data {get; set;}
    }
}