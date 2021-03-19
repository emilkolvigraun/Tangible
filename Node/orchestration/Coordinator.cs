using System.Collections.Generic;
using System.Linq;
using System;

namespace Node 
{
    public class Coordinator
    {
        private StateActor _actor = new StateActor();
        private ChangeQueue queue = new ChangeQueue();
        private bool _isLeader = false;
        private int _electionTimeout = Utils.GetRandomInt(0, 300);
        private long _lastHeartBeat = Utils.Millis;
        private long _previousHeartbeat = Utils.Millis;
        private string _currentLeader = "";
        private bool electionTerm = false;
        public enum State 
        {
            LEADER,
            FOLLOWER,
            CANDIDATE,
            SLEEPING
        }

        public void RunCoordinator()
        {
            Logger.Log(this.GetType().Name, "Coordinator is running.", Logger.LogLevel.INFO);
            while (true)
            {
                try 
                {
                    Utils.Wait(5);
                    State _State = GetState; 
                    _actor.Process(_State);
                
                } catch(Exception e)
                {
                    Logger.Log("RunCoordinator", e.Message, Logger.LogLevel.WARN);
                }
            }
        }

        public ChangeQueue ChangeQueue
        {
            get 
            {
                return queue;
            }
        }
        public void SetCurrentLeader(string n)
        {
            lock(_leader_lock)
            {
                _currentLeader = n;
            }
        }
        public string CurrentLeader
        {
            get 
            {
                lock(_leader_lock)
                {
                    return _currentLeader;
                }
            }
        }
        private State GetState
        {
            get 
            {
                State _state = State.SLEEPING;
                while (_state == State.SLEEPING)
                {
                    if (Ledger.Instance.Cluster.Count < 1) _state = State.SLEEPING;
                    else if (_isLeader) _state = State.LEADER;
                    else if (Utils.Millis > _lastHeartBeat + Params.HEARTBEAT_MS + _electionTimeout)
                    {
                        _state = State.CANDIDATE;
                    }
                    else _state = State.FOLLOWER;
                }
                return _state;
            }
        }
        public bool IsLeader 
        {
            get 
            {
                lock (_isLeader_lock)
                {
                    return _isLeader;   
                }
            }
        }
        public void ToggleLeadership(bool b)
        {
            lock(_isLeader_lock)
            {
                if (b)
                {
                    if(!Consumer.Instance.IsRunning) Consumer.Instance.Start(new string[]{Params.BROADCAST_TOPIC, Params.REQUEST_TOPIC});
                    _currentLeader = Params.NODE_NAME;
                    Logger.Log(Params.NODE_NAME, "Acting as Leader", Logger.LogLevel.IMPOR);           
                }
                else if (_isLeader!=b && !b) 
                {
                    Logger.Log(Params.NODE_NAME, "No longer acting as Leader", Logger.LogLevel.IMPOR);
                    Consumer.Instance.Stop();
                } else if (!b && CurrentLeader == null) Consumer.Instance.Stop();
                _isLeader = b;
            }
        }
        public void ResetHeartbeat()
        {
            lock (_hb_lock)
            {
                _previousHeartbeat = _lastHeartBeat;
                _lastHeartBeat = Utils.Millis;
            }
        }
        public long LeaderHeartbeat
        {
            get
            {
                lock(_hb_lock)
                {
                    return _lastHeartBeat - _previousHeartbeat;
                }
            }
        }        
        public void StartElectionTerm()
        {
            lock (_isLeader_lock)
            {
                electionTerm = true;
            }
        }
        public void StopElectionTerm()
        {
            lock (_isLeader_lock)
            {
                electionTerm = false;
            }
        }
        public bool IsElectionTerm
        {
            get 
            {
                lock(_isLeader_lock)
                {
                    return electionTerm;
                }
            }
        }
        private static readonly object _lock = new object();
        private readonly object _hb_lock = new object();
        private readonly object _isLeader_lock = new object();
        private readonly object _leader_lock = new object();
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