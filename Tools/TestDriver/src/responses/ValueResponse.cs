using System.Collections.Generic;

namespace TangibleDriver 
{
    public class ValueResponse 
    {
        public long Timestamp {get; set;}

        public string Epoch {get; set;}

        public string Value {get; set;}

        public string Measure {get; set;}

        public string Unit {get; set;}

        public string Protocol {get; set;}

        public string Point {get; set;}
    }
}