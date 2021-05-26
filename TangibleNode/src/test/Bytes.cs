using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System;

namespace TangibleNode
{
    class Bytes 
    {
        public static void Run()
        {

            int bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Point(){ID = "sensor_999"}, Formatting.None)).Length;
            Console.WriteLine("Point bytes: " + bytes);
            ESBDataRequest dr = new ESBDataRequest(){
                Type = DataRequest._Type.WRITE,
                Priority = 2,
                Value = Params.STEP.ToString(),
                // T0 = Utils.Micros.ToString(),
                Benv = new Location(){
                    HasPoint = new List<Point>{new Point(){ID = "sensor_999"}
                }},
                ReturnTopic = "MyApplication"
            };
            Console.WriteLine(JsonConvert.SerializeObject(dr));
            int bytes_dr = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dr, Formatting.None)).Length;
            Console.WriteLine("DR bytes: " + bytes_dr);

            DataRequest action = new DataRequest(){
                Type = dr.Type,
                PointDetails = new Dictionary<string, List<string>> {{"awdawdadwawdawd", new List<string>{"sensor_999"}}},
                Image = "...........aadsasd...............asdasd............",
                Priority = dr.Priority,
                Value = dr.Value,
                ID = Utils.GenerateUUID(),
                Assigned = StateLog.Instance.Peers.ScheduleAction(),
                // T0 = dr.T0,
                // T1 = Utils.Micros.ToString(),
                ReturnTopic = dr.ReturnTopic
            };
            Call r0 = new Call() {
                ID = Utils.GenerateUUID(),
                Data = Encoder.EncodeDataRequest(action),
                Type = Call._Type.DATA_REQUEST
            };
            int bytes_r0 = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(r0, Formatting.None)).Length;
            Console.WriteLine("r0 bytes: " + bytes_dr);

            
            int bytes_rb = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new ProcedureCallBatch(){
                Batch = new List<Call>{r0},
                Completed = new HashSet<string>{"sensor_999"},
                Sender = Sender.Self,
                Step = 10000000000
            }, Formatting.None)).Length;
            Console.WriteLine("rb bytes: " + bytes_rb);       
        }
    }
}