
namespace Node 
{
    class Response 
    {
        public long T2 {get; set;}
        public long T1 {get; set;}
        public bool Status {get; set;} = false;
        public string Value {get; set;} = null;
        public string PointID {get; set;} = null;
    }
}