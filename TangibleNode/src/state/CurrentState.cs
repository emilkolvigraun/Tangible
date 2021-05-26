
namespace TangibleNode
{
    class CurrentState 
    {
        public TTimer Timer {get;} = new TTimer("mainLoop");
        private TState _state {get;} = new TState();
        private readonly object _object_lock = new object();

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

        public void CancelState()
        {
            _state.Cancel();
        }

        public void SetStateAsLeader()
        {
            _state.SetStateAsLeader();
        }

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