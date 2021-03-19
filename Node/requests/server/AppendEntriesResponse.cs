
namespace Node
{
    class AppendEntriesResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AR;
        public MetaNode Node {get; set;} = new MetaNode();
    }
}