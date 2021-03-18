
namespace Node 
{
    class RegistrationRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.RS;
        public byte[] Cert {get; set;} = Params.X509CERT_BYTES;
        public MetaNode[] Add {get; set;} = Ledger.Instance.ClusterCopy.AsNodeArray();
        public MetaNode Node {get; set;} = new MetaNode();

    }
}