using System.Collections.Generic;

namespace Node 
{
    class ActionRequest : IRequest
    {
        public string ID {get; set;}
        public RequestType TypeOf {get; set;}
        public ActionType Action {get; set;}
        public string User {get; set;}
        public int Priority {get; set;}
        public string ReturnTopic {get; set;}
        public GraphLocation Location {get; set;}
        public string Value {get; set;} = null;
        public long Timestamp {get; set;}
    }
}