
namespace Node
{
    class AppendEntriesRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AE;
        public string Name {get; set;} = Params.NODE_NAME;

        // Used for organizing the cluster
        public MetaNode[] Nodes {get; set;} = null;
        public string[] Remove {get; set;} = null;

        // Jobs that are disitrubted
        // and returned completed
        public Job[] Jobs {get; set;} = null;
    }
}