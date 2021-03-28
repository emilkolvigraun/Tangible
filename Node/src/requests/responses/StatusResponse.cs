
namespace Node 
{
    class StatusResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.STATUS;
        public bool Status {get; set;}
    }
}