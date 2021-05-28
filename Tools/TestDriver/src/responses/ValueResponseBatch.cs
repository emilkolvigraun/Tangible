using System.Collections.Generic;

namespace TangibleDriver
{
    public class ValueResponseBatch
    {
        public string UUID {get; set;}
        public HashSet<ValueResponse> Responses {get; set;}
    }
}