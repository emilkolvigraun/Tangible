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
        internal TDict<int, Queue<Action>> _dict = new TDict<int, Queue<Action>>();
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void Enqueue(Action action)
        {
            // _lock.EnterReadLock();
            // bool b = _dict.ContainsKey(action.Priority);
            // _lock.ExitReadLock();

            try 
            {
                // _lock.EnterWriteLock();
                if (!_dict.ContainsKey(action.Priority))
                {
                    _dict.Add(action.Priority, new Queue<Action>());
                }
            } catch {}
            // finally {_lock.ExitWriteLock();}

            // _lock.EnterReadLock();
            // bool contains = (_dict[action.Priority].Any((a) => a.ID == action.ID));
            // _lock.ExitReadLock();
            
            // _lock.EnterWriteLock();
            if(!_dict[action.Priority].Any((a) => a.ID == action.ID)) 
            {
                _dict[action.Priority].Enqueue(action);
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to self");
            }
            // _lock.ExitWriteLock();
        }

        public Action Dequeue()
        {
            Action ra = null;
            // _lock.EnterReadLock();
            if (_dict.Count < 1) return ra;

            foreach(int priority in _dict.Keys.OrderByDescending(i => i))
            {
                if (_dict[priority].Count < 1) continue;
                ra = _dict[priority].Dequeue();
                break;
            }
            // _lock.ExitReadLock();
            return ra;
        }

        public int Count 
        {
            get 
            {
                // _lock.EnterReadLock();
                // int c = _dict.Count;
                // _lock.ExitReadLock();
                // return c;
                int count = 0;
                _dict.Values.ToList().ForEach((q) => {count+=q.Count;});
                return count;
            }
        }

        public int PCount(int priority)
        {
            // _lock.EnterReadLock();
            if (!_dict.ContainsKey(priority)) return 0;
            int c = _dict[priority].Count;
            // _lock.ExitReadLock();
            return c;
        }
        
    }
}