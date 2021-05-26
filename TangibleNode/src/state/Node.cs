using System.Collections.Generic;

namespace TangibleNode
{
    class Node
    {
        public SynchronousClient Client {get;}
        private TDict<string, DataRequest> _tasks {get;}
        public TInt Heartbeat {get;}
        private Sender _node {get;}

        public Node(Sender node)
        {
            _node = node;
            Client = new SynchronousClient(node.Host, node.Port, node.ID);
            _tasks = new TDict<string, DataRequest>();
            Heartbeat = new TInt();
        }

        public Sender AsNode
        {
            get 
            {
                return _node;
            }
        }

        public DataRequest GetAction(string actionID)
        {
            if (_tasks.ContainsKey(actionID))
                return _tasks[actionID];
            return null;
        }

        public void AddAction(DataRequest action)
        {
            if (!_tasks.ContainsKey(action.ID))
            {
                _tasks.AddSafe(new KeyValuePair<string, DataRequest>(action.ID, action));
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to [node:"+action.Assigned+"]");
            } 
        }

        public void RemoveAction(string actionID)
        {
            if (_tasks.ContainsKey(actionID))
                _tasks.Remove(actionID);
        }

        public void ForEachAction(System.Action<DataRequest> action)
        {
            _tasks.ForEachRead((p) => action(p));
        }

        public int ActionCount
        {
            get 
            {
                return _tasks.Count;
            }
        }
    }
}