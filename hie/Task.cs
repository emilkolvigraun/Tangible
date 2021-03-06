using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Node
{ 
    class Task
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public enum Type
        {
            SUBSCRIBE,
            READ,
            WRITE
        }

        public Type TypeOf {get; set;}
        public BuildingEnvironment Benv {get; set;}
        public User User {get; set;}
        public string Driver {get; set;}
    }
}