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
            DataRequest dr = new DataRequest(){
                Type = Action._Type.WRITE,
                Priority = 2,
                Value = Params.STEP.ToString(),
                // T0 = Utils.Micros.ToString(),
                Benv = new Location(){
                    HasPoint = new List<Point>{new Point(){ID = "sensor_999"}
                }},
                ReturnTopic = "MyApplication"
            };
            int bytes_dr = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dr, Formatting.None)).Length;
            Console.WriteLine("DR bytes: " + bytes_dr);

            Action action = new Action(){
                Type = dr.Type,
                PointID = new List<string>{"sensor_999"},
                Image = "...........aadsasd...............asdasd............",
                Priority = dr.Priority,
                Value = dr.Value,
                ID = Utils.GenerateUUID(),
                Assigned = StateLog.Instance.Peers.ScheduleAction(),
                // T0 = dr.T0,
                // T1 = Utils.Micros.ToString(),
                ReturnTopic = dr.ReturnTopic
            };
            Request r0 = new Request() {
                ID = Utils.GenerateUUID(),
                Data = Encoder.EncodeAction(action),
                Type = Request._Type.ACTION
            };
            int bytes_r0 = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(r0, Formatting.None)).Length;
            Console.WriteLine("r0 bytes: " + bytes_dr);

            
            int bytes_rb = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new RequestBatch(){
                Batch = new List<Request>{r0},
                Completed = new HashSet<string>{"sensor_999"},
                Sender = Node.Self,
                Step = 10000000000
            }, Formatting.None)).Length;
            Console.WriteLine("rb bytes: " + bytes_rb);

            
        }
    }
}