
namespace Node 
{
    class RegistrationRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.REGISTRATION;
        public byte[] Cert {get; set;} = Params.X509CERT_BYTES;
        public Node _Node {get; set;}
    }
}