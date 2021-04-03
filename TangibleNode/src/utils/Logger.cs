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

        private bool _stateLogger = false;
        public static void EnableStateLogger()
        {
            lock(Instance._lock)
            {
                Instance._stateLogger=true;
            }
        }

        public static void WriteStateHeader()
        {
            lock(Instance._lock)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("ms,");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("log,");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("pc,");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("node,");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("step");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void Log(Tag tag, string message)
        {
            lock(_lock)
            {   
                if (_stateLogger)
                {
                    if (tag==Tag.COMMIT) WriteState();
                }
                else if (_tags.Contains(tag))
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
    
        public void WriteState()
        {
            lock(_lock)
            {
                long f = Utils.Millis;
                int i0 = StateLog.Instance.LogCount;
                int i1 = StateLog.Instance.PriorityQueue.Count;
                int i2 = StateLog.Instance.Peers.NodeCount;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(f+",");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(i0+",");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(i1+",");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(i2+",");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(Params.STEP+",");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(StateLog.Instance.GetBatchesBehind(Params.ID).Count+"");
                // Console.ForegroundColor = ConsoleColor.Yellow;
                // Console.WriteLine(i0+"");    
            }
        }
    }
}