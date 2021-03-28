
namespace Node 
{
    class VotingRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.VOTE;
        public string Vote {get; set;}
    }
}