
namespace Node 
{
    class CurrentState 
    {

        private long _lastHeartbeat = Utils.Millis;
        private long _previousHeartbeat = Utils.Millis;
        private int _electionTimeout = Utils.GetRandomInt(0, 300);
        private bool _isLeader = false;
        private bool electionTerm = false;

        public State GetState
        {
            get 
            {
                lock(_lock)
                {
                    State _state = State.SLEEPING;

                    if (Cluster.Instance.Count < 1) _state = State.SLEEPING;
                    else if (_isLeader) _state = State.LEADER;
                    else if (Utils.Millis > _lastHeartbeat + Params.HEARTBEAT_MS + _electionTimeout)
                    {
                        _state = State.CANDIDATE;
                    }
                    else _state = State.FOLLOWER;

                    _electionTimeout = Utils.GetRandomInt(0, 300);

                    return _state;
                }
            }

        }

        public void SetIsLeader(bool toggle)
        {
            lock(_lock)
            {
                if (toggle)
                {
                    if(!Consumer.Instance.IsRunning) Consumer.Instance.Start(new string[]{Params.BROADCAST_TOPIC, Params.REQUEST_TOPIC});
                    Logger.Log(Params.NODE_NAME, "Acting as Leader", Logger.LogLevel.IMPOR);           
                }
                else if (_isLeader!=toggle && !toggle) 
                {
                    Logger.Log(Params.NODE_NAME, "No longer acting as Leader", Logger.LogLevel.IMPOR);
                    Consumer.Instance.Stop();
                } else if (!toggle) Consumer.Instance.Stop();
                _isLeader = toggle;
            }
        }

        public bool IsLeader 
        {
            get 
            {
                lock (_lock)
                {
                    return _isLeader;   
                }
            }
        }

        public void ResetHeartbeat()
        {
            lock(_lock)
            {
                _previousHeartbeat = _lastHeartbeat;
                _lastHeartbeat = Utils.Millis;
            }
        }
        public void InitResetHeartbeat()
        {
            lock(_lock)
            {
                _previousHeartbeat = _lastHeartbeat;
                _lastHeartbeat = Utils.Millis+(Params.HEARTBEAT_MS*10);
            }
        }

        public long Heartbeat
        {
            get 
            {
                lock(_lock)
                {
                    return _lastHeartbeat - _previousHeartbeat;
                }
            }
        }

        public void StartElectionTerm()
        {
            lock (_lock)
            {
                electionTerm = true;
            }
        }
        public void StopElectionTerm()
        {
            lock (_lock)
            {
                electionTerm = false;
            }
        }
        public bool IsElectionTerm
        {
            get 
            {
                lock(_lock)
                {
                    return electionTerm;
                }
            }
        }

        // SINGLETON
        private static readonly object _lock = new object();
        private static CurrentState _instance = null;

        public static CurrentState Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if(_instance==null)_instance=new CurrentState();
                    return _instance;
                }
            }
        }
    }
}