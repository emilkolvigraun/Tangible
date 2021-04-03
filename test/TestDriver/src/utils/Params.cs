using System;

namespace TangibleDriver 
{
    class Params 
    {
        public static string HOST;
        public static int PORT;
        public static string ID;
        public static int TIMEOUT;

        public static string NODE_NAME;
        public static string NODE_HOST;
        public static int NODE_PORT;
        

        public static void LoadEnvironment()
        {
            ID = GetStrThrowIfMissing("ID");
            HOST = GetStrThrowIfMissing("HOST");
            PORT = GetIntThrowIfMissing("PORT");
            NODE_NAME = GetStrThrowIfMissing("NODE_NAME");
            NODE_HOST = GetStrThrowIfMissing("NODE_HOST");
            NODE_PORT = GetIntThrowIfMissing("NODE_PORT");
            TIMEOUT = GetIntOrSet("TIMEOUT", 500);
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