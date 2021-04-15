using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace TangibleDriver
{
    class TestRequestHandler : IRequestHandler
    {
        public ValueResponse OnRequest(PointRequest request)
        {
            Dictionary<string, (string Value, string Time)> r0 = new Dictionary<string, (string Value, string Time)>();
            request.PointIDs.ForEach((s) => {
                r0.Add(s, (request.Value, Utils.Micros.ToString()));
            });
            return new ValueResponse(){
                ActionID = request.ID,
                Message = r0
            };
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("HOST", "192.168.1.211");
            // Environment.SetEnvironmentVariable("ID", "asdfasdasd");
            // Environment.SetEnvironmentVariable("PORT", "8000");
            // Environment.SetEnvironmentVariable("NODE_HOST", "192.168.1.211");
            // Environment.SetEnvironmentVariable("NODE_NAME", "TcpNode0");
            // Environment.SetEnvironmentVariable("NODE_PORT", "5000");
            // Environment.SetEnvironmentVariable("BATCH_SIZE", "100");
            // Environment.SetEnvironmentVariable("TIMEOUT", "500");

            TestRequestHandler handler = new TestRequestHandler();
            Driver driver = new Driver(handler);

            driver.Start();
        }
    }
}
