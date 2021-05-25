using System.Collections.Generic;
using System.Linq;

namespace TangibleNode
{
    /// <summary>
    /// Syncronized Priority Queue
    /// </summary>
    internal class PQueue
    {
        internal TDict<int, Queue<DataRequest>> _dict = new TDict<int, Queue<DataRequest>>();
        private readonly object _lock = new object();

        public void Enqueue(DataRequest action)
        {
            lock (_lock)
            {
                try 
                {
                    if (!_dict.ContainsKey(action.Priority))
                    {
                        _dict.Add(action.Priority, new Queue<DataRequest>());
                    }
                } catch {}
                if(!_dict[action.Priority].Any((a) => a.ID == action.ID)) 
                {
                    _dict[action.Priority].Enqueue(action);
                    Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to self");
                }
            }
        }

        public DataRequest Dequeue()
        {
            lock(_lock)
            {
                DataRequest ra = null;
                if (_dict.Count < 1) return ra;

                foreach(int priority in _dict.Keys.OrderByDescending(i => i))
                {
                    if (_dict[priority].Count < 1) continue;
                    ra = _dict[priority].Dequeue();
                    break;
                }
                return ra;
            }
        }

        public int Count 
        {
            get 
            {
                lock(_lock)
                {
                    int count = 0;
                    _dict.Values.ToList().ForEach((q) => {count+=q.Count;});
                    return count;
                }
            }
        }

        public int PCount(int priority)
        {
            lock(_lock)
            {
                if (!_dict.ContainsKey(priority)) return 0;
                int c = _dict[priority].Count;
                return c;
            }
        }
        
    }
}