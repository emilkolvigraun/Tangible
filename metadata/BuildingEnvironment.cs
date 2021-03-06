using System.Collections.Generic;

namespace Node
{
    class BuildingEnvironment
    {
        public string Subject {get; set;}
        public Dictionary<string, BuildingEnvironment> Relation {get; set;} // predicate, object
    }
}