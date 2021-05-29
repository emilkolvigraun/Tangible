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

        public static void Write(string response)
        {
            string time = Utils.Micros.ToString();
            // string amount = response.Message.Keys.Count.ToString();
            lock(Instance._lock)
            {   

                string message = Encoder.SerializePretty(response);
                Instance.PrintLine(ConsoleColor.Yellow, message);
                Instance.PrintLine(ConsoleColor.Magenta, "----------");


                // foreach(KeyValuePair<string, (string Value, string Time)> entry in response.Message)
                // {
                //     Print(ConsoleColor.Green, time);
                //     Comma();
                //     Print(ConsoleColor.Yellow, entry.Key);
                //     Comma();
                //     Print(ConsoleColor.Red, entry.Value.Value);
                //     Comma();
                //     Print(ConsoleColor.Blue, entry.Value.Time);
                //     Comma();
                //     PrintLine(ConsoleColor.Green, response.Message.Count.ToString());
                //     // Comma();
                //     // PrintLine(ConsoleColor.Gray, amount);
                //     Console.ForegroundColor = ConsoleColor.White;
                // }
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

        public static void PrintHeader()
        {
            Instance.Print(ConsoleColor.Green, "time_received");
            Instance.Comma();
            Instance.Print(ConsoleColor.Cyan, "node");
            Instance.Comma();
            Instance.Print(ConsoleColor.Red, "point_value");
            Instance.Comma();
            Instance.Print(ConsoleColor.Blue, "point_value_time");
            Instance.Comma();
            Instance.PrintLine(ConsoleColor.Magenta, "points");
            Console.ForegroundColor = ConsoleColor.White;
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