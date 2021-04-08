namespace TangibleNode
{
    class Optional 
    {
        public int ElectionTimeoutTangeStart_MS {get; set;} = Params.GetIntOrSet("ELECTION_TIMEOUT_START", -1, 250);
        public int ElectionTimeoutTangeEnd_MS {get; set;} = Params.GetIntOrSet("ELECTION_TIMEOUT_END", -1, 500);
        public int Heartbeat_MS {get; set;} = Params.GetIntOrSet("HEARTBEAT_MS", -1, 1000);
        public int MaxRetries {get; set;} = Params.GetIntOrSet("MAX_RETRIES", -1, 5);
        public int Timeout_MS {get; set;} = Params.GetIntOrSet("TIMEOUT", -1, 500);
        public int BatchSize {get; set;} = 10;
        public int WaitBeforeStartConsumer_MS {get; set;} = 8000;
        public int WaitBeforeStart_MS {get; set;} = 0;
    }
}