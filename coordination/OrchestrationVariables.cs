using System;

namespace Node
{
    class OrchestrationVariables 
    {
        public static int HEARTBEAT_S;

        public static void Load()
        {
            string hb = Environment.GetEnvironmentVariable("HEARTBEAT_S");
            if (hb == null) HEARTBEAT_S = 2000;
            else HEARTBEAT_S = int.Parse(hb)*1000;
        }
    }
}