
namespace TangibleNode
{
    class CurrentState 
    {
        public TTimer Timer {get;} = new TTimer("mainLoop");
        private TState _state {get;} = new TState();
        private readonly object _object_lock = new object();
        private bool _candidate_resolve = false;
        // private bool _received_vote = false;

        // public bool ReceviedVote 
        // {
        //     get 
        //     {
        //         lock(_object_lock)
        //         {
        //             return _received_vote;
        //         }
        //     }
        // }

        public void SetCandidateResolve(bool b)
        {
            lock(_object_lock)
            {
                if (_candidate_resolve != b && b) Logger.Write(Logger.Tag.WARN, "Activated CANDIDATE_RESOLVE.");
                else if (_candidate_resolve != b && !b) Logger.Write(Logger.Tag.WARN, "Deactivated CANDIDATE_RESOLVE.");
                _candidate_resolve = b;
            }
        }

        public bool CandidateResolve
        {
            get 
            {
                lock(_object_lock) return _candidate_resolve;
            }
        }

        public bool IsLeader
        {
            get 
            {
                return (Get_State.State==State.LEADER);
            }
        }

        public (State State, bool TimePassed) Get_State 
        {
            get 
            {
                bool passed = Timer.HasTimeSpanPassed;
                return (_state.Get(passed, StateLog.Instance.Nodes.NodeCount < 1), passed);
            }
        }

        public void ActAsSleeper()
        {
            _state.SetStateAsSleeper();
        }

        public void CancelState()
        {
            _state.Cancel();
            // lock(_object_lock) _received_vote = true;
        }

        public void SetStateAsLeader()
        {
            SetCandidateResolve(false);
            // lock(_object_lock) _received_vote = true;
            _state.SetStateAsLeader();
        }

        // public void SetReceivedVote(bool b)
        // {
        //     lock(_object_lock) _received_vote = b;
        // }

        //-------------------CONSTRUCTER------------------//
        private static readonly object _lock = new object();
        private static CurrentState _instance = null;
        public static CurrentState Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance==null)_instance=new CurrentState();
                    return _instance;
                }
            }
        }
    }
}