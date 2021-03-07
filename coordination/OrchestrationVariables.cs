using System;

namespace Node
{
    class OrchestrationVariables 
    {
        public static int HEARTBEAT_MS;

        public static void Load()
        {
            string hb = Environment.GetEnvironmentVariable("HEARTBEAT_MS");
            if (hb == null) HEARTBEAT_MS = 500;
            else HEARTBEAT_MS = int.Parse(hb);
        }
    }
}