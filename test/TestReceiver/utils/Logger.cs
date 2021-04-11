using System;
using System.Collections.Generic;

namespace TestReceiver 
{
    class Logger 
    {
        private static readonly object _i_lock = new object();
        private readonly object _lock = new object();
        private static Logger _instance = null;

        private static Logger Instance 
        {
            get 
            {
                lock(_i_lock)
                {
                    if (_instance==null)_instance=new Logger();
                    return _instance;
                }
            }
        }

        public static void Write(ESBResponse response)
        {
            Instance.Log(response);
        }

        public static void PrintHeader()
        {
            Instance.Print(ConsoleColor.Green, "time_received");
            Instance.Comma();
            Instance.Print(ConsoleColor.Cyan, "node");
            Instance.Comma();
            Instance.Print(ConsoleColor.Yellow, "point_id");
            Instance.Comma();
            Instance.Print(ConsoleColor.Red, "point_value");
            Instance.Comma();
            Instance.Print(ConsoleColor.Blue, "point_value_time");
            Instance.Comma();
            // Instance.Print(ConsoleColor.Magenta, "T0,T1,T2,T3,T4");
            Instance.PrintLine(ConsoleColor.Magenta, "node_time");
            // Instance.Comma();
            // Instance.Print(ConsoleColor.Gray, "amount");
            // Instance.Comma();
            // Instance.PrintLine(ConsoleColor.Green, "extra");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Log(ESBResponse response)
        {
            string time = Utils.Millis.ToString();
            string n = response.Node;
            // string amount = response.Message.Keys.Count.ToString();
            lock(_lock)
            {   
                foreach(KeyValuePair<string, (string Value, string Time)> entry in response.Message)
                {
                    Print(ConsoleColor.Green, time);
                    Comma();
                    Print(ConsoleColor.Cyan, n);
                    Comma();
                    Print(ConsoleColor.Yellow, entry.Key);
                    Comma();
                    Print(ConsoleColor.Red, entry.Value.Value);
                    Comma();
                    Print(ConsoleColor.Blue, entry.Value.Time);
                    Comma();
                    PrintLine(ConsoleColor.Magenta, response.Timestamp);
                    // Comma();
                    // PrintLine(ConsoleColor.Gray, amount);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                // string value = response.Value;
                // string T0 = response.T0;
                // string T1 = response.T1;
                // string T2 = response.T2;
                // string T3 = response.T3;
                // string T4 = response.Timestamp.ToString();
                // string T5 = Utils.Micros.ToString();
                // string node = response.Node;
                // string point = response.Point;

                // Print(ConsoleColor.Green, value);
                // Comma();
                // Print(ConsoleColor.Red, node);
                // Comma();
                // Print(ConsoleColor.Yellow, T0);
                // Comma();
                // Print(ConsoleColor.Blue, T1);
                // Comma();
                // Print(ConsoleColor.Magenta, T2);
                // Comma();
                // Print(ConsoleColor.Green, T3);
                // Comma();
                // Print(ConsoleColor.Red, T4);
                // Comma();
                // PrintLine(ConsoleColor.Yellow, T5);
                // Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void Print(ConsoleColor color, string msg)
        {
            Console.ForegroundColor = color;
            Console.Write(msg);
        }
        private void PrintLine(ConsoleColor color, string msg)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
        }

        private void Comma()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(",");
        }
    }
}