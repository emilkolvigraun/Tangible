using System;

namespace Node 
{
    class DriverTransmit_test 
    {
        public static void Run()
        {
            NodeClient n = new NodeClient();
            for (int i = 0; i < 10; i++)
            {
                n.Run("192.168.1.237", 6000, "test123", new Execute(){
                    TypeOfAction = ActionType.WRITE,
                    Value = "123"
                });
                n.Run("192.168.1.237", 6000, "test123", new RunAsRequest(){
                    JobID = "123"
                });
            }

            Environment.Exit(0);
        }
    }
}