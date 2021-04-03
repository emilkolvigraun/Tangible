
namespace Server 
{
    class Response : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.TEST_RESPONSE;
        public long T3 {get; set;}
        public long T2 {get; set;}
        public long T1 {get; set;}
        public long T0 {get; set;}
        public bool Status {get; set;} = false;
        public string Value {get; set;} = null;
        public string Jobs {get; set;}
        public string Heartbeat {get; set;}
        public string Cluster {get; set;}
        public string Name {get; set;}
    }
}