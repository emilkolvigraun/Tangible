
namespace TangibleNode
{
    class SyncParams 
    {
          // DEBUGGING
        public long STEP = Params.STEP;
        public int Hertz;
    
        public string Test_receiver_host;
        public int Test_receiver_port;


        // Server and client variables
        public int Max_retries;
        public int Connection_timeout;
        public int Batch_size;

        // RAFT variables
        public int Election_timeout_start;
        public int Election_timeout_end;
        public int Heartbeat_ms;

        // ESB variables
        public string Broadcast_topic;
        public string Request_topic;    
    }
}