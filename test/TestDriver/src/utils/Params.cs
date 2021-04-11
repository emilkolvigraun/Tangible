using System;

namespace TangibleDriver 
{
    class Params 
    {
        // Tangible variables
        public static string HOST;
        public static int PORT;
        public static string ID;
        // Tangible variables
        public static string NODE_HOST;
        public static int NODE_PORT;
        public static string NODE_NAME;
        public static int BATCH_SIZE;


        // Server and client variables
        public static int TIMEOUT;

        public static void LoadEnvironment()
        {
            BATCH_SIZE = GetIntThrowIfMissing("BATCH_SIZE");
            HOST = GetStrThrowIfMissing("HOST");
            PORT = GetIntThrowIfMissing("PORT");
            ID = GetStrThrowIfMissing("ID");
            NODE_HOST = GetStrThrowIfMissing("NODE_HOST");
            NODE_PORT = GetIntThrowIfMissing("NODE_PORT");
            NODE_NAME = GetStrThrowIfMissing("NODE_NAME");
            TIMEOUT = GetIntThrowIfMissing("TIMEOUT");            
        }

        private static string GetStrThrowIfMissing(string var)
        {
            string str = Environment.GetEnvironmentVariable(var);
            if (str==null||str==string.Empty) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
            return str;
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
    }
}