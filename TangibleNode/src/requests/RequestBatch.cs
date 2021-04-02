using System.Collections.Generic;

namespace TangibleNode
{
    public class RequestBatch 
    {
        public List<Request> Batch {get; set;}
        public List<string> Completed {get; set;}
        public Node Sender {get; set;}
    }
}