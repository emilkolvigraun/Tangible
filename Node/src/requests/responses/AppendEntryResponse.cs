using System.Collections.Generic;

namespace Node 
{
    class AppendEntryResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.APPEND_RES;

        public bool Status {get; set;}

        public List<string> Completed {get; set;}
    }
}