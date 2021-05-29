using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TangibleNode
{
    class TInt 
    {
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private int _value {get; set;} = 0;

        public void Reset()
        {
            CurrentState.Instance.SetHIEVar(true);
            _lock.EnterWriteLock();
            _value = 0;
            _lock.ExitWriteLock();
        }

        public void Increment()
        {
            _lock.EnterWriteLock();
            _value++;
            _lock.ExitWriteLock();
        }

        public void Assign(int newValue)
        {
            _lock.EnterWriteLock();
            _value = newValue;
            _lock.ExitWriteLock();
        }

        public int Value
        {
            get 
            {
                _lock.EnterReadLock();
                int v = _value;
                _lock.ExitReadLock();
                return v;
            }
        }
    }
}