
namespace Node 
{
    class VotingRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.VT;
        public string Vote {get; set;} = Ledger.Instance.Vote;
        public BasicNode[] Nodes {get; set;}
    }
}