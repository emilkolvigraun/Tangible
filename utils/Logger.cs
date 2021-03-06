using System;

namespace Node 
{
    class Logger
    {
        public static void Log(string sender, string message)
        {
            Console.WriteLine(Utils.Millis.ToString() + " | " + sender.PadRight(15) + " | " + message + ".");
        
        }

    }
}