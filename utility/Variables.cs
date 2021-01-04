
using System;
using System.IO;
using System.Collections.Generic;

namespace EC.MS 
{

    class Variables 
    {
        public static string API_KEY = null;
        public static string IP_ADDRESS = null;
        public static string MAIN_FILE = null;
        public static int PORT = -1;
        public static Dictionary<string, string[]> SOURCES = null;
        public static Dictionary<string, string[]> SINKS = null;

        public static EdgeClient LoadConfig()
        {
            IP_ADDRESS = Environment.GetEnvironmentVariable("W_IP");
            API_KEY = Environment.GetEnvironmentVariable("W_KEY");
            string sources = Environment.GetEnvironmentVariable("W_SOURCE");
            string sinks = Environment.GetEnvironmentVariable("W_SINK");

            if (!int.TryParse(Environment.GetEnvironmentVariable("W_PORT"), out PORT)){
                return null;
            }

            if ( IP_ADDRESS == null || PORT == -1 || sources == null || sinks == null){
                return null;
            }

            DefaultLog.GetInstance(new List<LogLevel> { LogLevel.ALL } ).Log(LogLevel.INFO, "Loaded alle configuration parameters. ");

            string[] _sources = ParseInOut(sources);
            
            foreach(string source in _sources)
            {
                string[] source_info = source.Split("=");
                if (source_info.Length != 3) return null;
                if (SOURCES == null ) SOURCES = new Dictionary<string, string[]>();
                try 
                {
                    SOURCES.Add(source_info[0], new string[] {source_info[1], source_info[2]});
                }
                catch (Exception)
                {
                    // TODO: Handle exception
                    return null;
                }
            }

            if (SOURCES == null) 
            { 
                DefaultLog.GetInstance().Log(LogLevel.INFO, "Failed to parse initial source(s).");
                return null;
            }

            DefaultLog.GetInstance().Log(LogLevel.INFO, "Parsed initial source(s).");


            EdgeClient client = null;
            string[] _sinks = ParseInOut(sinks);
            foreach(string sink in _sinks)
            {
                string[] sink_info = sink.Split("=");
                if (sink_info.Length != 3) return null;
                if (client == null ) client = new EdgeClient();
                try 
                {
                    if (sink_info[0].Contains("kafka"))
                    {
                        client.AddSink(
                            new KafkaSink(sink_info[1], sink_info[2])
                        );
                    }
                }
                catch (Exception)
                {
                    // TODO: Handle exception
                    return null;
                }
            }

            if (client == null) 
            {
                DefaultLog.GetInstance().Log(LogLevel.INFO, "Failed to parse initial sink(s).");
                return null;
            } 

            DefaultLog.GetInstance().Log(LogLevel.INFO, "Parsed initial sink(s).");    
            
            return client;
        }

        private static string[] ParseInOut(string inout)
        {
            string[] _inout;
            if (inout.Split(" ").Length > 1){
                _inout = inout.Split(" ");
            } else {
                _inout = new string[] { inout };
            }
            return _inout;
        }
    }

}