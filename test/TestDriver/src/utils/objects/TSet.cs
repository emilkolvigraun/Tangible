using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace TangibleDriver 
{
    /// <summary>
    /// Syncronized Set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class TSet<T>
    {
        private HashSet<T> _complete = new HashSet<T>();
        internal ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public bool Contains(T ID)
        {
            _lock.EnterReadLock();
            bool contains = _complete.Contains(ID);
            _lock.ExitReadLock();
            return contains;
        }

        public void ForEachWrite(System.Action<T> action)
        {
            foreach(T t in _complete)
                action(t);
        }
        
        public void ForEachRead(System.Action<T> action)
        {
            foreach(T t in _complete)
                action(t);
        }

        public bool Add(T ID)
        {
            bool r = false;


            if (ID == null)
                return r;

            _lock.EnterWriteLock();
            try
            {
                if (!_complete.Contains(ID))
                {
                    _complete.Add(ID);
                }

                r = true;
            }
            catch
            {
                r = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return r;
        }

        public bool Remove(T ID)
        {
            bool r = false;

            if (ID == null)
                return r;

            _lock.EnterWriteLock();
            try
            {
                if (_complete.Contains(ID))
                {
                    _complete.Remove(ID);
                    r = true;
                }
            }
            catch 
            {
                r = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return r;
        }

        public List<T> ToList()
        {
            _lock.EnterReadLock();
            HashSet<T> _copy = _complete;
            _lock.ExitReadLock();
            return _copy.ToList();
        }   

    }
}