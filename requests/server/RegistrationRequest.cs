
namespace Node 
{
    class RegistrationRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.RS;
        public byte[] Cert {get; set;} = Params.X509CERT_BYTES;
        public string Host {get; set;} = Params.ADVERTISED_HOST_NAME;
        public int Port {get; set;} = Params.PORT_NUMBER;
        public string Name {get; set;} = Params.NODE_NAME;
        public double Usage {get; set;} = Params.USAGE;
        public BasicNode[] Nodes {get; set;} = Ledger.Instance.Cluster.AsBasicNodes();
    }
}