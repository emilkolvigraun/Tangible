using System.Collections.Generic;

namespace TangibleNode
{
    class Location
    {
        public string Name {get; set;}
        public List<Location> LocationOf {get; set;}
        public List<Point> HasPoint {get; set;}
    }
}