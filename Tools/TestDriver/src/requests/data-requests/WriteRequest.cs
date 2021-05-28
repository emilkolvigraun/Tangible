using System.Collections.Generic;
using System.Linq;

namespace TangibleDriver
{
    public class WriteRequest
    {
        public string UUID {get; set;}
        public string Value {get; set;}
        public List<string> Points {get; set;}

        public static WriteRequest Parse(DataRequest request)
        {
            List<WriteRequest> requests = new List<WriteRequest>();
            // "emilkolvigraun/tangible-test-driver
            // DemoNode1_emilkolvigraun_tangible_test_driver_0
            foreach(KeyValuePair<string, List<string>> info in request.PointDetails)
            {
                if (info.Key.Contains(Params.IMAGE))
                {
                    return new WriteRequest{
                        UUID = request.ID,
                        Value = request.Value,
                        Points = info.Value
                    };
                }
            }
            return null;
        }
    }
}