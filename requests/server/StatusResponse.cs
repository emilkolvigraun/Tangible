
namespace Node 
{
    class StatusResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.ST;
        public bool Status {get; set;}
    }
}