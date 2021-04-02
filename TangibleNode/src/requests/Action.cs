using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace TangibleNode
{
    public class Action
    {
        public enum _Type
        {
            READ, WRITE, SUBSCRIBE
        }

        [JsonConverter(typeof(StringEnumConverter))] 
        public _Type Type {get; set;}
        public List<string> PointID {get; set;}
        public string Image {get; set;}
        public string Value {get; set;}
        public int Priority {get; set;}
        public string ID {get; set;}
        public string Assigned {get; set;}
        public string T0  {get; set;}
    }   
}