using System;
using System.Collections.Generic;

namespace TangibleNode 
{
    class Logger 
    {
        public enum Tag 
        {
            DEBUG,
            INFO,
            COMMIT,
            WARN,
            ERROR,
            FATAL
        }

        private HashSet<Tag> _tags = new HashSet<Tag>{Tag.DEBUG, Tag.COMMIT, Tag.INFO, Tag.WARN, Tag.ERROR, Tag.FATAL};


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

        public static void Write(Tag tag, string message)
        {
            Instance.Log(tag, message);
        }

        private void Log(Tag tag, string message)
        {
            lock(_lock)
            {
                if (_tags.Contains(tag))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(Utils.Millis+"|");            
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(tag.ToString().PadRight(6)+"|");            
                    SetColor(tag);
                    Console.WriteLine(message);         
                    Console.ForegroundColor = ConsoleColor.White;

                    if (tag==Tag.FATAL) Environment.Exit(0);   
                }
            }
        }

        private void SetColor(Tag tag)
        {
            switch(tag)
            {
                case Tag.DEBUG: 
                    Console.ForegroundColor = ConsoleColor.White; 
                    break;
                case Tag.INFO:
                    Console.ForegroundColor = ConsoleColor.Green; 
                    break;
                case Tag.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red; 
                    break;
                case Tag.FATAL:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta; 
                    break;
                case Tag.WARN:
                    Console.ForegroundColor = ConsoleColor.Magenta; 
                    break;
                case Tag.COMMIT:
                    Console.ForegroundColor = ConsoleColor.Blue; 
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White; 
                    break;
            }
        }
    
        public static void WriteState()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(Utils.Millis+",");            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(StateLog.Instance.Peers.NodeCount+",");      
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(StateLog.Instance.PriorityQueue.Count);      
        }
    }
}