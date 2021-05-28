using System.Collections.Generic;
using System;

namespace TangibleDriver
{
    class TestRequestHandler : IRequestHandler
    {
        private List<ValueResponseBatch> Responses = new List<ValueResponseBatch>();

        public void ProcessWrite(WriteRequest wrq) 
        { 
            ValueResponseBatch rr = new ValueResponseBatch
            {
                UUID = wrq.UUID,
                Responses = new HashSet<ValueResponse>()
            };

            foreach (string sensorID in wrq.Points)
            {
                rr.Responses.Add(new ValueResponse {
                    Timestamp = Utils.Millis,
                    Epoch = "millis",
                    Value = wrq.Value,
                    Measure = "temperature",
                    Unit = "celcius",
                    Protocol = "test",
                    Point = sensorID
                });   
            }

            Logger.Write(Logger.Tag.INFO, "Processed " + rr.Responses.Count.ToString() + " write requests");
            Responses.Add(rr);
        }

        public void ProcessRead(ReadRequest rrq) { }
        public void ProcessSubscribe(SubscribeRequest rrq) { }
        public void ProcessSubscribeStop(SubscribeRequest rrq) { }

        public ValueResponseBatch[] GetResponses()
        {
            ValueResponseBatch[] responses = new ValueResponseBatch[Responses.Count];
            // Responses.CopyTo(0, responses, 0, responses.Length);
            Array.Copy(Responses.ToArray(), responses, Responses.Count);
            Responses.Clear();
            Logger.Write(Logger.Tag.INFO, "Retrieved " + responses.Length.ToString() + " ValueResponses");
            return responses;
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

            Logger.Write(Logger.Tag.INFO, "ID -> " + Params.ID);
            Logger.Write(Logger.Tag.INFO, "ADDRESS -> " + Params.HOST+":"+Params.PORT);
            Logger.Write(Logger.Tag.INFO, "NODE -> " + Params.NODE_NAME);
            Logger.Write(Logger.Tag.INFO, "IMAGE -> " + Params.IMAGE);
            Logger.Write(Logger.Tag.INFO, "-----------");

            driver.Start();
        }
    }
}
