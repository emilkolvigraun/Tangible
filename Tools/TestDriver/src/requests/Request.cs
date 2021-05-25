using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TangibleDriver 
{
    public class Request
    {
        public enum _Type 
        {
            POINT
        }
        public string ID {get; set;}
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;} = _Type.POINT;
        public byte[] Data {get; set;}
    }
}