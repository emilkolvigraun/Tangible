using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleDriver 
{
    public class Request
    {

        public enum _Type 
        {
            ACTION,
            DRIVER_RESPONSE
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public byte[] Data {get; set;}
    }
}