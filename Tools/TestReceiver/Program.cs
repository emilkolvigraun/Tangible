using System;

namespace TestReceiver
{
    class Program
    {

        public static string HOST;
        public static int PORT;

        static void Main(string[] args)
        {
            // Console.WriteLine(args[1]);
            // Environment.Exit(0);
            try 
            {
                HOST = args[1];
            } catch {
                HOST = args[0];
            }
            PORT = 4000;

            try {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Starting TestReciver on: " + HOST.ToString() + ":" + PORT.ToString());
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("-------------");
                Console.ForegroundColor = ConsoleColor.White;


                // Logger.PrintHeader();

                AsynchronousSocketListener listener = new AsynchronousSocketListener();
                listener.StartListening();
            } catch {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start.");
            }
        }
    }
}
