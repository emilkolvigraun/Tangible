
namespace Node 
{
    class ConnectNodes : IRequest 
    {
        public RequestType TypeOf {get; set;} = RequestType.CN;
        public BasicNode[] Nodes {get; set;} = Ledger.Instance.Cluster.AsBasicNodes();
    }
}