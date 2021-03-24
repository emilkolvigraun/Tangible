using System.Collections.Generic;

namespace Driver 
{
    class ProcessQueue
    {
    
        private Queue<IRequest> ExecuteQueue = new Queue<IRequest>();
    
        public void Enqueue(IRequest task)
        {
            lock(_queue_lock)
            {   
                ExecuteQueue.Enqueue(task);
            }
        }

        public IRequest Dequeue()
        {
            lock(_queue_lock)
            {
                if (ExecuteQueue.Count > 0)
                {
                    return ExecuteQueue.Dequeue();
                } 
                return null;
            }
        }

        private static ProcessQueue _instance = null;
        private static readonly object _lock = new object();
        private static readonly object _queue_lock = new object();
    
        public static ProcessQueue Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ProcessQueue();
                    }
                    return _instance;
                }
            }
        }
    }

}


