using System.Collections.Generic;

namespace TangibleNode
{
    class Response 
    {
        public Dictionary<string, bool> Status {get; set;}
        public List<string> Completed {get; set;}
        public bool HIEVar {get; set;} = CurrentState.Instance.HIEVar;

        // explicitly used for voting
        public byte[] Data {get; set;}
    }
}