using System.Collections.Generic;

namespace TangibleDriver
{
    public class Response 
    {
        public Dictionary<string, bool> Status {get; set;}

        // Not used by driver
        // TODO: REMOVE FROM DRIVER
        public List<string> Completed {get; set;}
        public bool HIEVar {get; set;} = true;
        public byte[] Data {get; set;}
    }
}