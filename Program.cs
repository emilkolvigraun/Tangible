using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EC.MS
{
    class Program
    {
        static void Main(string[] args)
        {
            EdgeClient running = Variables.LoadConfig();
            if (running == null) 
            {
                DefaultLog.GetInstance().Log(LogLevel.ERROR, "Error during startup.");
                DefaultLog.GetInstance().Log(LogLevel.ERROR, "Shutting dowm.");
                //Client.SendNotificationToMaster("Error during startup.");
            }
            else 
            {
                ProcessingModule module = new ProcessingModule(running);
                DefaultLog.GetInstance().Log(LogLevel.INFO, "Initialized processing module.");
                List<Task> tasks = new List<Task>();
                foreach(KeyValuePair<string, string[]> source in Variables.SOURCES)
                {
                    if (source.Key.ToLower().Contains("kafka"))
                    {
                        string[] _topics = source.Value[1].Split(",");
                        tasks.Add(new KafkaConsumer( source.Value[0], Utils.GetUniqueKey(12)).Subscribe(CleanTopics(_topics), module));
                    }
                    // else another source
                }
                        // t.Start();
                DefaultLog.GetInstance().Log(LogLevel.INFO, "Processing module now running.");
                Task.WaitAll(tasks.ToArray());
            }
        }

        private static string[] CleanTopics(string[] topics)
        {
            List<string> _topics = new List<string>();
            foreach(string top in topics)
            {
                if (!string.IsNullOrEmpty(top) || !string.IsNullOrWhiteSpace(top))
                {
                    _topics.Add(top);
                }
            }
            return _topics.ToArray();
        }
    }
}
