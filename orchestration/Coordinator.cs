using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace Node
{
    public class Coordinator
    {
        private StateActor _actor = new StateActor();
        private bool _isLeader = false;
        private int _electionTimeout = Utils.GetRandomInt(0, 300);
        private long _lastHeartBeat = Utils.Millis;
        private readonly object _hb_lock = new object();
        private readonly object _isLeader_lock = new object();
        private readonly object _leader_lock = new object();
        private readonly object _change_lock = new object();
        private string _currentLeader = "";

        private Queue<Change> _changeQueue;

        public enum State 
        {
            LEADER,
            FOLLOWER,
            CANDIDATE,
            SLEEPING
        }

        Coordinator()
        {
            _changeQueue = new Queue<Change>();
        }

        public void RunCoordinator()
        {
            Logger.Log(this.GetType().Name, "Coordinator is running.", Logger.LogLevel.INFO);
            long t0 = Utils.Millis;
            while (true)
            {
                try 
                {
                    if(Utils.Millis > t0+100)
                    {
                        State _State = GetState; 
                        _actor.Process(_State);
                        // Logger.Log(Params.NODE_NAME, "leader->" + _isLeader + " " + string.Join(", ", Ledger.Instance.ClusterCopy.GetAsToString()), Logger.LogLevel.WARN);
                        t0 = Utils.Millis;
                    }
                } catch(Exception e)
                {
                    Logger.Log("RunCoordinator", e.Message, Logger.LogLevel.WARN);
                }
            }
        }

        public void EnqueueRange(MetaNode[] _add = null, string[] _del = null)
        {
            if(_add!=null) foreach(MetaNode node in _add)
            {
                if (node.Name == Params.NODE_NAME || Ledger.Instance.ContainsKey(node.Name)) continue;
                    EnqueueChange(new Change(){
                        TypeOf = Change.Type.ADD,
                        Name = node.Name,
                        Host = node.Host,
                        Port = node.Port
                    });
            }
            if(_del!=null) foreach(string name in _del)
            {
                if (name == Params.NODE_NAME || Ledger.Instance.ContainsKey(name)) continue;                
                    EnqueueChange(new Change(){
                        TypeOf = Change.Type.DEL,
                        Name = name
                    });
                
            }
        }

        public void EnqueueChange(Change change)
        {
            lock(_change_lock)
            {
                ChangeQueue.Enqueue(change);
            }
        }

        public Change DequeueChange()
        {
            lock(_change_lock)
            {
                if (ChangeQueue.Count > 0)
                    return ChangeQueue.Dequeue();
                else    
                    return null;
            }

        }

        public Queue<Change> ChangeQueue
        {
            get 
            {
                lock (_change_lock)
                {
                    return _changeQueue;
                }
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
                    if (Ledger.Instance.ClusterCopy.Count < 1) _state = State.SLEEPING;
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
                _lastHeartBeat = Utils.Millis;
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

        private bool electionTerm = false;
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