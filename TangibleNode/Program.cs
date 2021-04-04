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
            // DEBUGGING

            Settings settings = default(Settings);            
            if (args.Length > 0)
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]));
                    if (settings.Optional==null) settings.Optional = new Optional();
                    if (args.Length > 1)
                    {
                        settings.ID = args[1];
                    }
                    if (args.Length > 2)
                    {
                        _enableStateLog = bool.Parse(args[2]);
                        if (_enableStateLog)
                        {
                            Logger.EnableStateLogger();
                        }
                    }
                    Console.WriteLine(args[0] +" : "+JsonConvert.SerializeObject(settings, Formatting.Indented));
                    Logger.Write(Logger.Tag.INFO, "Loaded settings.");
                } catch
                {
                    Logger.Write(Logger.Tag.FATAL, "Failed to load settings.");
                }
            } else Logger.Write(Logger.Tag.INFO, "No settings provided.");

            // parse the settings
            Params.LoadEnvironment(settings);

            // DEBUGGING
            if (_enableStateLog)
            {
                Logger.WriteStateHeader();
            }

            // run the node
            new TangibleNode(settings).Start();
        }
    }
}
