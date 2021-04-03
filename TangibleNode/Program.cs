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
            // DEBUGGING
            Logger.EnableStateLogger();

            Settings settings = default(Settings);            
            if (args.Length > 0)
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]));
                    if (settings.Optional==null) settings.Optional = new Optional();
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
            Logger.WriteStateHeader();

            // run the node
            new TangibleNode(settings).Start();
        }
    }
}
