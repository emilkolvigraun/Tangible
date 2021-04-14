using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace TangibleDriver
{
    class TestRequestHandler : IRequestHandler
    {
        public ValueResponse OnRequest(PointRequest request)
        {
            int count = 1;
            // string t0 = Utils.Micros.ToString();
            Dictionary<string, (string Value, string Time)> r0 = new Dictionary<string, (string Value, string Time)>();
            request.PointIDs.ForEach((s) => {
                r0.Add(s, (request.Value, Utils.Micros.ToString()));
                count++;
            });
            // Logger.Write(Logger.Tag.INFO, "Received point request of " + request.PointIDs.Count + ", action: " + request.ID);
            // Console.WriteLine("dict size: " + Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(r0, Formatting.None)).Length);
            return new ValueResponse(){
                ActionID = request.ID,
                Message = r0,
                NodeReceived = request.Received
                // T0123 = request.T0+","+request.T1+","+request.T2+","+request.T3
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
