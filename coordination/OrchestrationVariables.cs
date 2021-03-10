using System;

namespace Node
{
    class OrchestrationVariables 
    {
        public static int HEARTBEAT_MS;

        public static void Load()
        {
            HEARTBEAT_MS = Utils.GetRandomInt(300, 500);
        }
    }
}