using System;
using System.Collections.Generic;

namespace TangibleDriver
{
    class TestRequestHandler : IRequestHandler
    {
        public ValueResponse OnRequest(PointRequest request)
        {
            int count = 1;
            string t0 = Utils.Micros.ToString();
            Dictionary<string, (string Value, string Time)> r0 = new Dictionary<string, (string Value, string Time)>();
            request.PointIDs.ForEach((s) => {
                r0.Add(s, (request.Value, t0));
                count++;
            });
            Logger.Write(Logger.Tag.INFO, "Received point request of " + request.PointIDs.Count);
            return new ValueResponse(){
                ActionID = request.ID,
                Message = r0,
                T0123 = request.T0+","+request.T1+","+request.T2+","+request.T3
            };
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
