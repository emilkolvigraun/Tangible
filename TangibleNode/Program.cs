using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TangibleNode
{
    class Program
    {
        static void Main(string[] args)
        {
            bool _enableStateLog = false;

            Settings settings = default(Settings);            


            if (args.Length > 0)
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]));
                    if (settings.Optional==null) settings.Optional = new Optional();
                    if (args.Length > 2)
                    {
                        settings.ID = args[2];
                    }
                    Console.WriteLine(args[0] +" : "+JsonConvert.SerializeObject(settings, Formatting.Indented));
                    Logger.WriteHeader();
                    Logger.Write(Logger.Tag.INFO, "Loaded settings.");
                } catch
                {
                    Logger.Write(Logger.Tag.FATAL, "Failed to load settings.");
                }
            } else Logger.Write(Logger.Tag.INFO, "No settings provided.");

            Utils.Sleep(settings.Optional.WaitBeforeStart_MS);
            // parse the settings
            Params.LoadEnvironment(settings);

            if (args.Length > 1)
            {
                _enableStateLog = bool.Parse(args[1]);
                if (_enableStateLog)
                {
                    FileLogger.EnableFileLog();
                }
            }

            FileLogger.Instance.CreateLogFile();
            FileLogger.Instance.WriteHeader();
            // run the node
            new TangibleNode(settings).Start();
        }
    }
}
