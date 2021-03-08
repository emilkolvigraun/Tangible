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
            ERROR,
            FATAL,
            DEBUG,
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
                Levels = new List<LogLevel>(){LogLevel.DEBUG};
            }
        }
        public static void Log(string sender, string message, LogLevel tag)
        {
            if ((Levels.ToList().Contains(tag) || Levels.ToList().Contains(LogLevel.DEBUG)) && !Levels.ToList().Contains(LogLevel.NONE))
            {
                Console.WriteLine(Utils.Millis.ToString() + " | " + tag.ToString().PadRight(5) + " | " + sender.PadRight(15) + " | " + message + ".");
            }
        }
    }
}