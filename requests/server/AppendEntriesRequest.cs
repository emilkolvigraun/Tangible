
namespace Node
{
    class AppendEntriesRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AE;

        public MetaNode[] Add {get; set;} = Ledger.Instance.ClusterCopy.AsNodeArray();
        public string[] Flag {get; set;} = Coordinator.Instance.Flagged.ToArray();
    }
}