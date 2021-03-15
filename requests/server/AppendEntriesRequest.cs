
namespace Node
{
    class AppendEntriesRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AE;
        public string Name {get; set;} = Params.NODE_NAME;
        public MetaNode[] Add {get; set;} = Ledger.Instance.ClusterCopy.AsNodeArray();
        public string[] Flag {get; set;} = Coordinator.Instance.FlaggedCopy.ToArray();
    }
}