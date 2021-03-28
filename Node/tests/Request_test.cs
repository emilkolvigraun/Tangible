using System;

namespace Node 
{
    class Request_test 
    {
        public static void Run()
        {
            ActionRequest r0 = new ActionRequest()
            {
                TypeOf = RequestType.WRITE,
                Action = ActionType.WRITE,
                Location = new GraphLocation(){
                    ID = "MMMI",
                    LocationOf = new GraphLocation()
                    {
                        ID = "ROOM-xyz",
                        HasPoint = new GraphPoint(){
                            ID = "sensor-xyz"
                        }
                    }
                },
                Value = "22",
                Priority = 2,
                ReturnTopic = "MyApplication",
                User = "Admin",
            };

            Console.WriteLine(r0.EncodeRequestStr());
            byte[] r1 = r0.EncodeRequest();
            try 
            {
                Console.WriteLine(r1.DecodeRequest().GetString());
            } catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                Console.WriteLine("should throw exception");
            }
            try 
            {
                Console.WriteLine(r1.GetString().DecodeRequest().EncodeRequestStr());
            } catch (Exception e)
            {
                Console.WriteLine("should NOT throw exception " + e.Message);
            }
        }
    }
}