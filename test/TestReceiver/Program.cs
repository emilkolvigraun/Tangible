using System;

namespace TestReceiver
{
    class Program
    {

        public static string HOST;
        public static int PORT;

        static void Main(string[] args)
        {
            HOST = "192.168.1.237";
            PORT = 4000;

            Logger.PrintHeader();

            AsynchronousSocketListener listener = new AsynchronousSocketListener();
            listener.StartListening();
        }
    }
}
