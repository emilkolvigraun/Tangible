using System.Collections.Generic;
using System.Linq;
using System;

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

        public void EnqueueRange(MetaNode[] _add = null, string[] _del = null)
        {
            try
            {
                if(_add!=null) foreach(MetaNode node in _add)
                {
                    lock (_change_lock)
                    {
                        if (node.Name != Params.NODE_NAME && !Ledger.Instance.ContainsKey(node.Name) && !Queue.Any(x => x.Name == node.Name)) 
                        {
                            EnqueueChange(new Change(){
                                TypeOf = Change.Type.ADD,
                                Name = node.Name,
                                Host = node.Host,
                                Port = node.Port
                            });
                            Logger.Log("_add", "add[node:" + node.Name + "] appended to change queue", Logger.LogLevel.IMPOR);
                        }
                    }
                }
            } catch(Exception e)
            {
                Logger.Log("EnqueueRange_add", e.Message, Logger.LogLevel.ERROR);
            }
            try 
            {
                if(_del!=null) foreach(string name in _del)
                {
                    lock (_change_lock)
                    {
                        if (Ledger.Instance.ContainsKey(name) && !Queue.Any(x => x.Name == name))
                        {
                            EnqueueChange(new Change(){
                                TypeOf = Change.Type.DEL,
                                Name = name
                            });
                            Logger.Log("_del", "delete[node:" + name + "] appended to change queue", Logger.LogLevel.IMPOR);
                        }
                    }                
                }
            } catch(Exception e)
            {
                Logger.Log("EnqueueRange_del", e.Message, Logger.LogLevel.ERROR);
            }
        }
    }
}