using System.Collections.Generic;

namespace Node 
{
    public class ActionRequest : IRequest
    {
        public RequestType TypeOf {get; set;}
        public string User {get; set;}
        public int Priority {get; set;}
        public string ReturnTopic {get; set;}
        public GraphLocation Location {get; set;}
        public string Value {get; set;} = null;
    }
}