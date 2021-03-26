
namespace Driver 
{
    class RequestResponse : IRequest 
    {
        public RequestType TypeOf {get; set;} = RequestType.RR;

        public bool Status {get; set;}
        public string Value {get; set;}
        public string JobId {get; set;}
    }
}