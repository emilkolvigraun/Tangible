using System.Collections.Generic;

namespace TangibleDriver
{
    public class RequestBatch 
    {
        public List<Request> Batch {get; set;}
        public List<string> Completed {get; set;}
        public Node Sender {get; set;}

        // DEBUGGING
        public long Step {get; set;} = 0;
    }
}