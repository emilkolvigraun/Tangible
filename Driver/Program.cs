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

            Thread serverThread = new Thread(() => {
                NodeServer.RunServer();
            });

            serverThread.Start();

            Console.WriteLine("Starting with params:");
            Console.WriteLine(Params.MACHINE_NAME + ", " + Params.HOST_NAME + ":" + Params.PORT_NUMBER.ToString());



            ProcessLoop.Instance.Run(new Driver());
        }
    }
}
