using System;
using System.Collections.Generic;

namespace TangibleNode 
{
    class Params 
    {

        // DEBUGGING
        public static long STEP = 0;
        public static int DIE_AS_FOLLOWER;
        public static int DIE_AS_LEADER;
        public static int WAIT_BEFORE_START;
        public static decimal HERTZ;
        public static bool RUN_HIE;
        public static string TEST_RECEIVER_HOST;
        public static int TEST_RECEIVER_PORT;


        // Tangible variables
        public static string HOST;
        public static int PORT;
        public static string ID;

        // Server and client variables
        public static int MAX_RETRIES;
        public static int TIMEOUT;
        public static int BATCH_SIZE;

        // RAFT variables
        public static int ELECTION_TIMEOUT_START;
        public static int ELECTION_TIMEOUT_END;
        public static int HEARTBEAT_MS;

        // ESB variables
        public static string BROADCAST_TOPIC;
        public static string REQUEST_TOPIC;    
        
        // HIE variables
        public static string DOCKER_REMOTE_HOST;
        public static string DOCKER_HOST;
        public static int DRIVER_PORT_RANGE_START;
        public static int DRIVER_PORT_RANGE_END;
        private static Queue<int> _unusedPorts = new Queue<int>();

        public static void LoadEnvironment(Settings settings)
        {
            HOST = GetStrThrowIfMissing("HOST", settings.Host);
            PORT = GetIntThrowIfMissing("PORT", settings.Port);
            ID = GetStrThrowIfMissing("ID", settings.ID);

            DIE_AS_FOLLOWER = GetIntThrowIfMissing("DIE_AS_FOLLOWER", settings.Testing.DieAsFollower_MS);
            DIE_AS_LEADER = GetIntThrowIfMissing("DIE_AS_LEADER", settings.Testing.DieAsLeader_MS);
            WAIT_BEFORE_START = GetIntOrSet("WAIT_BEFORE_START_CONSUMER", settings.Optional.WaitBeforeStartConsumer_MS, 10000, 2000);
            TEST_RECEIVER_HOST = settings.Testing.TestReceiverHost;
            TEST_RECEIVER_PORT = settings.Testing.TestReceiverPort;
            HERTZ = settings.Testing.Frequency_Hz;
            RUN_HIE = settings.Testing.RunHIE;

            TIMEOUT = GetIntOrSet("TIMEOUT", settings.Optional.Timeout_MS, 500);
            MAX_RETRIES = GetIntOrSet("MAX_RETRIES", settings.Optional.MaxRetries, 10);
            BATCH_SIZE = settings.Optional.BatchSize;
            
            REQUEST_TOPIC = GetStrThrowIfMissing("REQUEST_TOPIC", settings.RequestTopic);
            BROADCAST_TOPIC = GetStrThrowIfMissing("BROADCAST_TOPIC", settings.BroadcastTopic);
            
            ELECTION_TIMEOUT_START = GetIntOrSet("ELECTION_TIMEOUT_START", settings.Optional.ElectionTimeoutTangeStart_MS, 250);
            ELECTION_TIMEOUT_END = GetIntOrSet("ELECTION_TIMEOUT_END", settings.Optional.ElectionTimeoutTangeEnd_MS, 500);
            HEARTBEAT_MS = GetIntOrSet("HEARTBEAT_MS", settings.Optional.Heartbeat_MS, 1000, 300);
            
            DRIVER_PORT_RANGE_START = GetIntOrSet("DRIVER_PORT_RANGE_START", settings.DriverPortRangeStart, 8000);
            DRIVER_PORT_RANGE_END = GetIntOrSet("DRIVER_PORT_RANGE_END", settings.DriverPortRangeEnd, 8100);
            DOCKER_REMOTE_HOST = GetStrThrowIfMissing("DOCKER_REMOTE_HOST", settings.DockerRemoteHost);
            DOCKER_HOST = GetStrThrowIfMissing("DOCKER_HOST", settings.DriverHostName);

            for(int i = DRIVER_PORT_RANGE_START; i < DRIVER_PORT_RANGE_END; i++)
            {
                _unusedPorts.Enqueue(i);
            }
        }
        public static int GetUnusedPort()
        {
            if (_unusedPorts.Count < 1) Logger.Write(Logger.Tag.FATAL, "No more free ports!");
            return _unusedPorts.Dequeue();
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
            if (i!=-1&&i<0) Logger.Write(Logger.Tag.FATAL, var + " NOT SET. ");
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