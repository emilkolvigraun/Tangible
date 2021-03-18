using System;
using System.Linq;
using System.Collections.Generic;

namespace Node 
{
    class Logger
    {
        public enum LogLevel 
        {
            INFO,
            WARN,
            ERROR,
            FATAL,
            DEBUG,
            IMPOR,
            NONE
        }
        public static List<LogLevel> Levels;
        public static void LoadLogLevel()
        {
            string llvl = Environment.GetEnvironmentVariable("LOG_LEVEL");
            if (llvl != null)
            {
                string[] levelArr = llvl.Split(",");
                Levels = new List<LogLevel>();
                foreach(string s in levelArr)
                {
                    if (s.ToLower().Contains("debug")) Levels.Add(LogLevel.DEBUG);
                    else if (s.ToLower().Contains("error")) Levels.Add(LogLevel.ERROR);
                    else if (s.ToLower().Contains("info")) Levels.Add(LogLevel.INFO);
                    else if (s.ToLower().Contains("none")) Levels.Add(LogLevel.NONE);
                }
            } else
            {
                Levels = new List<LogLevel>(){LogLevel.INFO, LogLevel.WARN, LogLevel.ERROR, LogLevel.FATAL, LogLevel.IMPOR}; //LogLevel.DEBUG};//
            }
        }
        public static void Log(string sender, string message, LogLevel tag)
        {
            if ((tag == LogLevel.IMPOR || Levels.ToList().Contains(tag) || Levels.ToList().Contains(LogLevel.DEBUG)) && !Levels.ToList().Contains(LogLevel.NONE))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(Utils.Millis.ToString());
                PrintSeperator();
                GetColor(tag);
                Console.Write(tag.ToString().PadRight(5));
                PrintSeperator();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(sender.PadRight(16));
                PrintSeperator();
                GetColor(tag);
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (tag == LogLevel.FATAL) Environment.Exit(1);
        }

        private static void PrintSeperator()
        {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" | ");
        }

        private static void GetColor(LogLevel tag)
        {
            if (tag == LogLevel.WARN) Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (tag == LogLevel.FATAL) Console.ForegroundColor = ConsoleColor.DarkMagenta;
            if (tag == LogLevel.ERROR) Console.ForegroundColor = ConsoleColor.Red;
            if (tag == LogLevel.DEBUG) Console.ForegroundColor = ConsoleColor.Gray;
            if (tag == LogLevel.INFO) Console.ForegroundColor = ConsoleColor.Green;
            if (tag == LogLevel.IMPOR) Console.ForegroundColor = ConsoleColor.Cyan;
        }
    }
}