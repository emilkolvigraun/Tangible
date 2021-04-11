using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TangibleDriver 
{
    /// <summary>Thread safe queue with slim lock</summary>
    /// <typeparam name="T"></typeparam>
    public class TQueue<T>
    {
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        internal Queue<T> _queue = new Queue<T>();

        public void Enqueue(T obj)
        {
            _lock.EnterWriteLock();
            _queue.Enqueue(obj);
            _lock.ExitWriteLock();
        }

        public int Count
        {
            get 
            {
                int c = 0;
                _lock.EnterReadLock();
                c = _queue.Count;
                _lock.ExitReadLock();
                return c;
            }
        }

        public T Dequeue()
        {
            _lock.EnterReadLock();
            T t;
            bool b = _queue.TryDequeue(out t);
            _lock.ExitReadLock();
            return t;
        }
    }
}