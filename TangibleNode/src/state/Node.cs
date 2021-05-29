using System.Collections.Generic;

namespace TangibleNode
{
    class Node
    {
        public SynchronousClient Client {get;}
        private TDict<string, DataRequest> _tasks {get;}
        public TInt Heartbeat {get;}
        private Credentials _node {get;}

        public bool HIEVar {get;} = true;
        public bool Signal {get;} = false;

        public Node(Credentials node)
        {
            _node = node;
            Client = new SynchronousClient(node.Host, node.Port, node.ID);
            _tasks = new TDict<string, DataRequest>();
            Heartbeat = new TInt();
        }

        public Credentials AsNode
        {
            get 
            {
                return _node;
            }
        }

        public DataRequest GetEntry(string actionID)
        {
            if (_tasks.ContainsKey(actionID))
                return _tasks[actionID];
            return null;
        }

        public void AddEntry(DataRequest action)
        {
            if (!_tasks.ContainsKey(action.ID))
            {
                _tasks.AddSafe(new KeyValuePair<string, DataRequest>(action.ID, action));
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to [node:"+action.Assigned+"]");
            } 
        }

        public void RemoveEntry(string actionID)
        {
            if (_tasks.ContainsKey(actionID))
                _tasks.Remove(actionID);
        }

        public void ForEachEntry(System.Action<DataRequest> action)
        {
            _tasks.ForEachRead((p) => action(p));
        }

        public int Entries
        {
            get 
            {
                return _tasks.Count;
            }
        }
    }
}