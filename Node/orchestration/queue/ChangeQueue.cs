using System.Collections.Generic;

namespace Node 
{
    public class ChangeQueue
    {
        private readonly object _change_lock = new object();
        private Queue<Change> _changeQueue = new Queue<Change>();

        public void EnqueueChange(Change change)
        {
            lock(_change_lock)
            {
                _changeQueue.Enqueue(change);
            }
        }

        public Change DequeueChange()
        {
            lock(_change_lock)
            {
                if (_changeQueue.Count > 0)
                    return _changeQueue.Dequeue();
                else    
                    return null;
            }
        }

        public Queue<Change> Queue
        {
            get 
            {
                lock (_change_lock)
                {
                    return _changeQueue;
                }
            }
        }
    }
}