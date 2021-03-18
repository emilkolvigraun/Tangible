
namespace Node
{
    class BroadcastRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.BC;
        public MetaNode Node {get; set;} = new MetaNode();
    }
}