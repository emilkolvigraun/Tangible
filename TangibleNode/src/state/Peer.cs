using System.Collections.Generic;

namespace TangibleNode
{
    class Peer
    {
        public SynchronousClient Client {get;}
        private TDict<string, Action> _tasks {get;}
        public TInt Heartbeat {get;}
        private Node _node {get;}

        public Peer(Node node)
        {
            _node = node;
            Client = new SynchronousClient(node.Host, node.Port, node.ID);
            _tasks = new TDict<string, Action>();
            Heartbeat = new TInt();
        }

        public Node AsNode
        {
            get 
            {
                return _node;
            }
        }

        public void AddAction(Action action)
        {
            if (!_tasks.ContainsKey(action.ID))
            {
                _tasks.AddSafe(new KeyValuePair<string, Action>(action.ID, action));
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to [node:"+action.Assigned+"]");
            } 
        }

        public void RemoveAction(string actionID)
        {
            if (_tasks.ContainsKey(actionID))
                _tasks.Remove(actionID);
        }

        public void ForEachAction(System.Action<Action> action)
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