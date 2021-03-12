
namespace Node 
{
    class CertificateResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.CT;
        public byte[] Cert {get; set;} = Params.X509CERT_BYTES;
        public double Usage {get; set;} = Params.USAGE;
        public BasicNode[] Nodes {get; set;} = Ledger.Instance.Cluster.AsBasicNodes();
    }
}