using System;
using System.Threading;

namespace Driver
{
    class Program
    { 
        static void Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("HOST", "192.168.1.237");
            // Environment.SetEnvironmentVariable("PORT", "6000");
            // Environment.SetEnvironmentVariable("NAME", "test123");
            Params.LoadParams();
            // Driver driver = new Driver();

            // Thread serxverThread = new Thread(() => {
            // }); 
            // Thread driverThread = new Thread(() => {
            //     driver.Run();
            // }); 

            // serverThread.Start();

            Console.WriteLine("Starting with params:");
            Console.WriteLine(Params.MACHINE_NAME + ", " + Params.HOST_NAME + ":" + Params.PORT_NUMBER.ToString());

            // driverThread.Start();
            // ProcessLoop.Instance.Run(driver);
            NodeServer.RunServer();
        }
    }
}
