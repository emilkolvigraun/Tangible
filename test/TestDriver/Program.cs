using System;

namespace TangibleDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("ID", "123");
            // Environment.SetEnvironmentVariable("HOST", "192.168.1.237");
            // Environment.SetEnvironmentVariable("PORT", "6000");
            // Environment.SetEnvironmentVariable("NODE_NAME", "TcpNode1");
            // Environment.SetEnvironmentVariable("NODE_HOST", "192.168.1.237");
            // Environment.SetEnvironmentVariable("NODE_PORT", "5001");
            // Environment.SetEnvironmentVariable("TIMEOUT", "1000");

            Params.LoadEnvironment();
            AsynchronousSocketListener listener = new AsynchronousSocketListener(
                new Handler()
            );
            listener.StartListening();
        }
    }
}
