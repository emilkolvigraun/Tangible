using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TangibleNode
{
    /// <summary>
    /// Syncronized Priority Queue
    /// </summary>
    internal class PQueue
    {
        internal Dictionary<int, Queue<Action>> _dict = new Dictionary<int, Queue<Action>>();
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void Enqueue(Action action)
        {
            _lock.EnterReadLock();
            bool b = _dict.ContainsKey(action.Priority);
            _lock.ExitReadLock();

            if (!b)
            {
                _lock.EnterWriteLock();
                _dict.Add(action.Priority, new Queue<Action>());
                _lock.ExitWriteLock();
            }

            _lock.EnterWriteLock();
            _dict[action.Priority].Enqueue(action);
            _lock.ExitWriteLock();
        }

        public Action Dequeue()
        {
            Action ra = null;
            _lock.EnterReadLock();
            if (_dict.Count < 1) return ra;

            foreach(int priority in _dict.Keys.OrderByDescending(i => i))
            {
                if (_dict[priority].Count < 1) continue;
                ra = _dict[priority].Dequeue();
                break;
            }
            _lock.ExitReadLock();
            return ra;
        }

        public int Count 
        {
            get 
            {
                _lock.EnterReadLock();
                int c = _dict.Count;
                _lock.ExitReadLock();
                return c;
            }
        }

        public int PCount(int priority)
        {
            _lock.EnterReadLock();
            if (!_dict.ContainsKey(priority)) return 0;
            int c = _dict[priority].Count;
            _lock.ExitReadLock();
            return c;
        }
        
    }
}