using System.Collections.Generic;

namespace TangibleNode
{
    public class ProcedureCallBatch 
    {
        public List<Call> Batch {get; set;}
        public HashSet<string> Completed {get; set;}
        public Credentials Sender {get; set;}

        // DEBUGGING
        public long Step {get; set;} = Params.STEP;
    }
}