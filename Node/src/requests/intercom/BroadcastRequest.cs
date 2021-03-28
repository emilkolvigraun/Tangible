
namespace Node
{
    class BroadcastRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.BROADCAST;

        // Has itself as a Node
        public Node _Node {get; set;}
    }
}