using System;

namespace Node
{
    class EsbVariables
    {
        public static string KAFKA_BROKERS;
        public static string CLUSTER_ID;
        public static string BROADCAST_TOPIC;
        public static string REQUEST_TOPIC;
        public static int WAIT_TIME_S;

        public static void Load()
        {
            string bt = Environment.GetEnvironmentVariable("BROADCAST_TOPIC");
            if (bt == null) 
            {
                throw new ArgumentException("BROADCAST_TOPIC is undefined.");
            } else BROADCAST_TOPIC = bt;
            string kb = Environment.GetEnvironmentVariable("KAFKA_BROKERS");
            if (kb == null) 
            {
                throw new ArgumentException("KAFKA_BROKERS is undefined.");
            } else KAFKA_BROKERS = kb;
            string cid = Environment.GetEnvironmentVariable("CLUSTER_ID");
            if (cid == null) 
            {
                throw new ArgumentException("CLUSTER_ID is undefined.");
            } else CLUSTER_ID = cid;
            string rt = Environment.GetEnvironmentVariable("REQUEST_TOPIC");
            if (rt == null) 
            {
                throw new ArgumentException("REQUEST_TOPIC is undefined.");
            } else REQUEST_TOPIC = rt;
            string wt = Environment.GetEnvironmentVariable("WAIT_TIME_S");
            if (wt == null) WAIT_TIME_S = 3000;
            else WAIT_TIME_S = (int.Parse(wt)*1000)+3000;
        }
    }
}