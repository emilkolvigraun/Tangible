
namespace Node 
{
    class CertificateResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.CT;
        public byte[] Cert {get; set;} = Params.X509CERT_BYTES;
        public Job[] Jobs {get; set;} = Scheduler.Instance.Jobs;
    }
}