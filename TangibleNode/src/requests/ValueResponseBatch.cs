using System.Collections.Generic;

namespace TangibleNode
{
    public class ValueResponseBatch
    {
        public string UUID {get; set;}
        public HashSet<ValueResponse> Responses {get; set;}
    }
}