using System.Collections.Generic;

namespace TangibleNode
{
    class Response 
    {
        public Dictionary<string, bool> Status {get; set;}
        public List<string> Completed {get; set;}

        // explicitly used for voting
        public byte[] Data {get; set;}
    }
}