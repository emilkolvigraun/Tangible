using System.Collections.Generic;
using System.Linq;

namespace Node
{
    public class Coordinator
    {

        public enum State 
        {
            LEADER,
            FOLLOWER,
            CANDIDATE
        }

        private long _lastHeartbeat = Utils.Millis;
        private long _currentPenalty = Utils.Millis;
        private long _cancel = Utils.Millis;
        private bool _isLeader = false;
        private string _leader = null;
        private StateActor Actor = new StateActor();
        private List<string> _Flagged;
        private static readonly object _flag_lock = new object();
        private static readonly object _cancel_lock = new object();

        public void RunCoordinator()
        {
            _Flagged = new List<string>();
            Logger.Log(this.GetType().Name, "Coordinator is running.", Logger.LogLevel.INFO);
            while (true)
            {
                State _State = GetState(); 
                Actor.Process(_State);
            }
        }

        public void SetPenalty(int time = -1)
        {
            if (time == -1) time = Params.HEARTBEAT_MS;
            _currentPenalty = Utils.Millis + time;
        }

        public bool IsCancelled
        {
            get 
            {
                lock (_cancel_lock)
                {
                    return _cancel > Utils.Millis;
                }
            }
        }

        public void Cancel()
        {
            lock (_cancel_lock)
            {
                _cancel = Utils.Millis + Params.HEARTBEAT_MS*2;
            }
        }

        private State GetState()
        {
            while (true)
            {
                if(Ledger.Instance.Quorum > 0) break;
            }
            if (_isLeader && !IsCancelled)
            {
                return State.LEADER;
            } 
            else if (Utils.Millis - _lastHeartbeat > Params.HEARTBEAT_MS && Utils.Millis > _currentPenalty) 
            {
                SetPenalty(Params.HEARTBEAT_MS);
                return State.CANDIDATE;
            } 
            else
            {
                return State.FOLLOWER;
            } 
        }
        
        public void AddFlag(string name)
        {
            lock (_flag_lock)
            {
                Flagged.Add(name);
            }
        }

        public void RemoveFlag(string name)
        {
            lock (_flag_lock)
            {
                Flagged.Remove(name);
            }
        }

        public string Vote 
        {
            get 
            {
                // TODO: NOT YET IMPLEMENTED VOTATION METRICS
                return Params.NODE_NAME;
            }
        }

        public List<string> Flagged
        {
            get 
            {
                lock(_flag_lock)
                {
                    return _Flagged;
                }
            }
        }

        public List<string> FlaggedCopy
        {
            get 
            {
                lock(_flag_lock)
                {
                    return Flagged.ToList();
                }
            }
        }
        public void ToggleLeadership(bool v0, string leader)
        {
            lock(_leader_lock)
            {
                if (v0 != _isLeader || _leader != leader)
                {
                    _isLeader = v0;
                    _leader = leader;
                    Logger.Log(this.GetType().Name, "Set leader to " + leader + ", is that me? " + (v0?"Yes":"No"), Logger.LogLevel.INFO);
                    if (_isLeader) Logger.Log(leader, "I am the leader.", Logger.LogLevel.IMPOR);
                    if (!_isLeader) Consumer.Instance.Stop();
                    else if (_isLeader) Consumer.Instance.Start(new string[]{Params.BROADCAST_TOPIC, Params.REQUEST_TOPIC});
                }
            }
        }

        public string Leader
        {
            get 
            {
                lock(_leader_lock)
                {
                    return _leader;
                }
            }
        }

        private static readonly object _leader_lock = new object();
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