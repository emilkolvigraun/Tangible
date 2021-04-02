using System.Collections.Generic;
using System.Linq;

namespace TangibleNode
{
    class StateLog
    {
        public NodePeers Peers {get;} = new NodePeers();
        public PQueue PriorityQueue {get;} = new PQueue(); 

        private TDict<string, TDict<string, Request>> BatchesBehind {get;} = new TDict<string, TDict<string, Request>>();
        private TDict<string, List<string>> ActionsCompleted {get;} = new TDict<string, List<string>>();
        private TSet<string> MyCompletedActions {get;} = new TSet<string>();
        private object _batch_lock = new object();
        private object _action_lock = new object();

        public List<string> Leader_GetActionsCompleted(string peerID)
        {
            lock (_action_lock)
            {
                if (!ActionsCompleted.ContainsKey(peerID)) return new List<string>();
                List<string> behind = ActionsCompleted[peerID];
                return behind;
            }
        }

        public void Leader_RemoveActionsCompleted(string peerID, string action)
        {
            if (ActionsCompleted.ContainsKey(peerID))
                ActionsCompleted[peerID].Remove(action);
        }

        public void Leader_AddActionCompleted(string actionID)
        {
            lock(_action_lock)
            {
                Peers.ForEachPeer((p) => {
                    p.RemoveAction(actionID);
                    if (!this.ActionsCompleted.ContainsKey(p.Client.ID))
                        this.ActionsCompleted.Add(p.Client.ID, new List<string>());
                    this.ActionsCompleted[p.Client.ID].Add(actionID);
                });
                
            }
        }

        public void Follower_AddActionCompleted(string actionID)
        {
            lock(_action_lock)
            {
                MyCompletedActions.Remove(actionID);
                Peers.ForEachPeer((p) => {
                    p.RemoveAction(actionID);
                });
                Logger.Write(Logger.Tag.COMMIT, "Commited " + actionID);
            }
        }

        public void Follower_MarkActionCompleted(string actionID)
        {
            Logger.Write(Logger.Tag.INFO, "Marked " + actionID + " completed.");
            MyCompletedActions.Add(actionID);
        }

        public List<string> Follower_GetCompletedActions()
        {
            List<string> completed = new List<string>();
            MyCompletedActions.ForEachRead((a) => {
                completed.Add(a);
            });
            return completed;
        }

        public List<Request> GetBatchesBehind(string peerID)
        {
            lock (_batch_lock)
            {
                if (!BatchesBehind.ContainsKey(peerID)) return new List<Request>();
                List<Request> behind = BatchesBehind[peerID].Values.ToList();
                return behind;
            }
        }

        public void RemoveBatchBehind(string peerID, Request request)
        {
            if (BatchesBehind.ContainsKey(peerID))
            {
                BatchesBehind[peerID].Remove(request.ID);
            } 
        }

        public void ClearPeerLog(string peerID)
        {
            if (BatchesBehind.ContainsKey(peerID))
            {
                BatchesBehind.Remove(peerID);
            } 
            if (ActionsCompleted.ContainsKey(peerID))
            {
                ActionsCompleted.Remove(peerID);
            }
        }

        public void AddRequestBehind(string peerID, Request request)
        {
            lock(_batch_lock)
            {
                if (!BatchesBehind.ContainsKey(peerID))
                    BatchesBehind.Add(peerID, new TDict<string, Request>());
                BatchesBehind[peerID].Add(request.ID, request);
            }
        }

        public void AddRequestBehindToAllBut(string peerID, Request request)
        {
            Peers.ForEachPeer((p)=>{
                if (p.Client.ID != peerID)
                {
                    AddRequestBehind(p.Client.ID, request);
                }
            });
        }
        public void AddRequestBehindToAll(Request request)
        {
            Peers.ForEachPeer((p)=>{
                AddRequestBehind(p.Client.ID, request);
            });
        }

        public void AppendAction(Action action)
        {
            if (action.Assigned == Params.ID)
            {
                PriorityQueue.Enqueue(action);
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to self");
            } else 
            {
                Peers.AppendAction(action.Assigned, action);
                Logger.Write(Logger.Tag.COMMIT, "Committed [action:"+action.ID.Substring(0,10)+"...] to [node:"+action.Assigned+"]");
            }
        }

        private static readonly object _lock = new object();
        private static StateLog _instance = null;
        public static StateLog Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance==null)_instance=new StateLog();
                    return _instance;
                }
            }
        }
    }
}