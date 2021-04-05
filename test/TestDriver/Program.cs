using System;
using System.Collections.Generic;

namespace TangibleDriver
{
    class TestRequestHandler : IRequestHandler
    {
        public List<ValueResponse> OnRequest(PointRequest request)
        {
            List<ValueResponse> response = new List<ValueResponse>();
            int count = 1;
            request.PointIDs.ForEach((s) => {
                bool complete = request.PointIDs.Count<=count?true:false;
                response.Add(
                    new ValueResponse(){
                        ActionID = request.ID,
                        Complete = complete,
                        ReturnTopic = request.ReturnTopic,
                        Point = s,
                        T0 = request.T0,
                        T1 = request.T1,
                        T2 = request.T2,
                        Timestamp = Utils.Millis.ToString(),
                        Value = request.Value
                    }
                );
                count++;
            });
            return response;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            TestRequestHandler handler = new TestRequestHandler();
            Driver driver = new Driver(handler);

            driver.Start();
        }
    }
}
