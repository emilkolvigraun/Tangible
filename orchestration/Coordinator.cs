namespace Node
{
    class Coordinator
    {

        enum State 
        {
            LEADER,
            FOLLOWER,
            CANDIDATE
        }

        private long _lastHeartbeat = Utils.Millis;
        private long _currentPenalty = Utils.Millis;
        private bool _isLeader = false;

        public void RunCoordinator()
        {
            Logger.Log(this.GetType().Name, "Coordinator is running.", Logger.LogLevel.INFO);
            while (true)
            {
                Logger.Log(this.GetType().Name, "State: " + GetState().ToString() + ", Quorum: " + Ledger.Instance.Quorum + " " + string.Join(", ", Ledger.Instance.Cluster.AsBasicNodes().AsStringArray()), Logger.LogLevel.DEBUG);
            }
        }

        public void SetPenalty(int time = -1)
        {
            if (time == -1) time = Params.HEARTBEAT_MS;
            _currentPenalty = Utils.Millis + time;
        }

        private State GetState()
        {
            State _state = State.FOLLOWER;
            while (_state == State.FOLLOWER)
            {
                if (Ledger.Instance.Quorum < 1)
                {
                    _state = State.FOLLOWER;
                }
                else if (_isLeader)
                {
                    _state = State.LEADER;
                } 
                else if (Utils.Millis - _lastHeartbeat > Params.HEARTBEAT_MS && Utils.Millis > _currentPenalty) 
                {
                    SetPenalty(Params.HEARTBEAT_MS);
                    _state = State.CANDIDATE;
                } 
                else
                {
                    _state = State.FOLLOWER;
                } 
            }
            return _state;
        }

        private static readonly object _lock = new object();
        private static Coordinator _instance = null;

        public static Coordinator Instance 
        {
            get 
            {
                lock (_lock)
                {
                    if (_instance == null) _instance = new Coordinator();
                    return _instance;
                }
            }
        }
    }
}