using System.Collections.Generic;

namespace TangibleDriver
{
    public class ProcedureCallBatch 
    {
        public List<Call> Batch {get; set;}
        public HashSet<string> Completed {get; set;}
        public Credentials Sender {get; set;}
    }
}