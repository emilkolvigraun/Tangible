using System.Threading;

namespace TangibleDriver
{
    class TTimer 
    {
        private long _time {get; set;} = Utils.Millis;
        private long _prev_time {get; set;} = Utils.Millis;
        private long _cycle {get; set;}
        private bool _begun {get; set;} = false;
        private long _timespan {get; set;} = 0;
        private string ID {get;}
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public TTimer(string ID)
        {
            this.ID = ID;
        }

        /// <summary>Bool to indicate whether the timer is active or not.</summary>
        public bool Active
        {
            get 
            {
                _lock.EnterReadLock();
                bool active = _begun;
                _lock.ExitReadLock();
                return active;
            }
        }

        /// <summary>Starts the timer. It will now verify whether the timespan has passed.</summary>
        public void Begin()
        {
            Reset();
            _lock.EnterWriteLock();
            _begun = true;
            _lock.ExitWriteLock();
            Logger.Write(Logger.Tag.INFO, "Timer -> " + ID + ", started.");
        }

        /// <summary> Stops the timer. Any timespan check returns false. </summary>
        public void End()
        {
            Reset();
            _lock.EnterWriteLock();
            _begun = false;
            _lock.ExitWriteLock();
            Logger.Write(Logger.Tag.INFO, "Timer -> " + ID + ", stopped.");
        }

        /// <summary>check whether the time has passed based in defined ms</summary>
        public bool HasTimePassed(int milliseconds)
        {
            _lock.EnterReadLock();
            bool started = _begun;
            bool passed = Utils.Millis >= _time+milliseconds;
            _lock.ExitReadLock();
            return (started && passed);
        }

        /// <summary>checks whether the time has passed in relation to current timespan</summary>
        public bool HasTimeSpanPassed
        {
            get 
            {
                _lock.EnterReadLock();
                bool started = _begun;
                _lock.ExitReadLock();
                if(!started) return false;
                _lock.EnterReadLock();
                bool passed = Utils.Millis >= _time;
                _lock.ExitReadLock();
                return passed;
            }
        }

        /// <summary>Sets the timespan to a ranged timespan between start and end</summary>
        public void SetTimeSpan(int start, int end)
        {
            _lock.EnterWriteLock();
            _timespan = Utils.GetRandomInt(start, end+1);
            _lock.ExitWriteLock();
        }

        /// <summary>Sets the timespan to a defined interval</summary>
        public void SetTimeSpan(int interval)
        {
            _lock.EnterWriteLock();
            _timespan = interval;
            _lock.ExitWriteLock();
        }

        /// <summary>Resets everything</summary>
        public void Reset(int penalty = 0)
        {
            if (penalty > 0) Logger.Write(Logger.Tag.DEBUG, "Timer -> " + ID + ", received penalty of " + penalty + "ms");
            _lock.EnterWriteLock();
            _prev_time = _time;
            _time = Utils.Millis+penalty;
            _lock.ExitWriteLock();
        }

        /// <summary>The time between the two last resets</summary>
        public long TimeBetween 
        { 
            get 
            {
                _lock.EnterReadLock();
                long diff = _time - _prev_time;
                _lock.ExitReadLock();
                return diff;
            }
        }

        /// <summary>Time since last reset</summary>
        public long TimePassed 
        { 
            get 
            {
                _lock.EnterReadLock();
                long diff = Utils.Millis - _time;
                _lock.ExitReadLock();
                return diff;
            }
        }

        /// <summary>Waits until timespan has run out</summary>
        public void Wait()
        {
            _lock.EnterReadLock();
            bool started = _begun;
            _lock.ExitReadLock();
            if (started)
            {
                bool passed = this.HasTimeSpanPassed;
                while (!passed)
                {
                    passed = this.HasTimeSpanPassed;
                    if (passed) break;
                }
            }
        }
    }
}