using System.Threading;
using System;

namespace TangibleNode
{
    class TState 
    {
        private State _state {get; set;} = State.SLEEPER;
        private State p_state {get; set;} = State.SLEEPER;
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public void Cancel()
        {
            _lock.EnterReadLock();
            State s0 = _state;
            _lock.ExitReadLock();
            _lock.EnterWriteLock();
            p_state = s0;
            _state = State.FOLLOWER;
            _lock.ExitWriteLock();
            LogIfChangeDetected();
        }

        public void SetStateAsLeader()
        {
            _lock.EnterReadLock();
            State s0 = _state;
            _lock.ExitReadLock();
            _lock.EnterWriteLock();
            p_state = s0;
            _state = State.LEADER;
            _lock.ExitWriteLock();
            LogIfChangeDetected();
        }
        public void SetStateAsSleeper()
        {
            _lock.EnterReadLock();
            State s0 = _state;
            _lock.ExitReadLock();
            _lock.EnterWriteLock();
            p_state = s0;
            _state = State.SLEEPER;
            _lock.ExitWriteLock();
            LogIfChangeDetected();
        }

        public State Get(bool timePassed, bool alone) 
        {
            _lock.EnterWriteLock();
            p_state = _state;
            if (_state==State.LEADER) _state = State.LEADER;
            else if (timePassed && !CurrentState.Instance.CandidateResolve) _state = State.CANDIDATE;
            else if (alone || CurrentState.Instance.CandidateResolve) _state = State.SLEEPER;
            else if (!CurrentState.Instance.CandidateResolve) _state = State.FOLLOWER;
            _lock.ExitWriteLock();

            return LogIfChangeDetected();
        }

        private State LogIfChangeDetected()
        {
            _lock.EnterReadLock();
            State state = _state;
            if (p_state != state)
                Logger.Write(Logger.Tag.WARN, "Changed state from " + p_state.ToString() + " to " + state.ToString());
            _lock.ExitReadLock();
            return state;
        }
    }
}