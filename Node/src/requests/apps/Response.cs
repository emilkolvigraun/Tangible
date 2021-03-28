
namespace Node 
{
    class Response 
    {
        public long T2 {get; set;}
        public long T1 {get; set;}
        public long T0 {get; set;}
        public bool Status {get; set;} = false;
        public string Value {get; set;} = null;
        public string Jobs {get; set;}
        public string Heartbeat {get; set;}
        public string Cluster {get; set;}
        public string Name {get; set;} = Params.NODE_NAME;
    }
}