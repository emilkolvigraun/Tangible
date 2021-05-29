using System.Collections.Generic;

namespace TangibleNode
{
    class Node
    {
        public SynchronousClient Client {get;}
        private TDict<string, DataRequest> _tasks {get;}
        public TInt Heartbeat {get;}
        private Credentials _node {get;}

        private bool hie_var = true;

        public bool HIEVar {
            get{
                return hie_var;
            } 
            set {
                if (hie_var != value)
                    Logger.Write(Logger.Tag.WARN, "HIEvar set to " + hie_var.ToString() + " for " + _node.ID.ToString());
                hie_var = value;
            }   
        } 
        

        public bool Signal {get; set;} = false;

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
                Logger.Write(Logger.Tag.COMMIT, "Committed [entry:"+action.ID.Substring(0,10)+"...] to [node:"+action.Assigned+"]");
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