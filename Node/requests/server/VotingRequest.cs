
namespace Node 
{
    class VotingRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.VT;
        public string Vote {get; set;} = Params.NODE_NAME;
    }
}