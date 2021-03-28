using System.Collections.Generic;

namespace Node 
{
    class AppendEntryRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.APPEND_REQ;
        public Dictionary<string, List<string>> LogDetach {get; set;} 
        public Dictionary<string, List<Request>> LogAppend {get; set;} 
        public List<Request> Enqueue {get; set;}  
        public List<string> Dequeue {get; set;}  
        public List<string> NodeDetach {get; set;} 
        public List<Node> NodeAttach {get; set;} 

        public HashSet<string> PriorityDetach {get; set;}
        public List<ActionRequest> PriorityAttach {get; set;}
    }
}