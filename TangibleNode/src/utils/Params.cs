using System;

namespace TangibleNode 
{
    class Params 
    {
        public static string HOST;
        public static int PORT;
        public static string ID;
        public static int MAX_RETRIES;
        public static int TIMEOUT;

        public static int ELECTION_TIMEOUT_START;
        public static int ELECTION_TIMEOUT_END;
        public static int HEARTBEAT_MS;

        public static string BROADCAST_TOPIC;
        public static string REQUEST_TOPIC;
        

        public static void LoadEnvironment(Settings settings)
        {
            HOST = GetStrThrowIfMissing("HOST", settings.Host);
            PORT = GetIntThrowIfMissing("PORT", settings.Port);
            ID = GetStrThrowIfMissing("ID", settings.ID);
            REQUEST_TOPIC = GetStrThrowIfMissing("REQUEST_TOPIC", settings.RequestTopic);
            BROADCAST_TOPIC = GetStrThrowIfMissing("BROADCAST_TOPIC", settings.BroadcastTopic);
            TIMEOUT = GetIntOrSet("TIMEOUT", settings.Optional.Timeout_MS, 500);
            MAX_RETRIES = GetIntOrSet("MAX_RETRIES", settings.Optional.MaxRetries, 10);
            ELECTION_TIMEOUT_START = GetIntOrSet("ELECTION_TIMEOUT_START", settings.Optional.ElectionTimeoutTangeStart_MS, 250);
            ELECTION_TIMEOUT_END = GetIntOrSet("ELECTION_TIMEOUT_END", settings.Optional.ElectionTimeoutTangeEnd_MS, 500);
            HEARTBEAT_MS = GetIntOrSet("HEARTBEAT_MS", settings.Optional.Heartbeat_MS, 1000, 300);
        }

        public static string GetStrThrowIfMissing(string var, string str)
        {
            if (str==null||str==string.Empty) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            return str;
        }
        private static string GetStrThrowIfMissing(string var)
        {
            string str = Environment.GetEnvironmentVariable(var);
            if (str==null||str==string.Empty) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            return str;
        }

        public static int GetIntThrowIfMissing(string var, int i)
        {
            if (i<0) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            return i;
        }
        private static int GetIntThrowIfMissing(string var)
        {
            string str = Environment.GetEnvironmentVariable(var);
            if (str==null||str==string.Empty) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            int i = int.Parse(str);
            if (i<0) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            return i;
        }
        
        private static int GetIntOrSet(string var, int alt)
        {
            string str = Environment.GetEnvironmentVariable(var);
            if (str==null||str==string.Empty||int.Parse(str) < 1) return alt;
            return int.Parse(str);
        }
        public static int GetIntOrSet(string var, int i, int alt, int minimum = 0)
        {
            if( i < minimum) return alt;
            if (i < 1) return alt;
            return i;
        }
    }
}