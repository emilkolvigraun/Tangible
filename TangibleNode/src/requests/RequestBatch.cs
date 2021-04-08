using System.Collections.Generic;

namespace TangibleNode
{
    public class RequestBatch 
    {
        public List<Request> Batch {get; set;}
        public HashSet<string> Completed {get; set;}
        public Node Sender {get; set;}

        // DEBUGGING
        public long Step {get; set;} = Params.STEP;
    }
}