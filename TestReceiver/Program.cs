using System;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("HOST", "192.168.1.237");
            Environment.SetEnvironmentVariable("NAME", "TestReceiverServer");
            Environment.SetEnvironmentVariable("PORT", "5006");
            Params.LoadParams();
            new Thread(()=>{
                WriteQueue.Instance.Run();
            }).Start();
            Serv.RunServer();
        }
    }
}
