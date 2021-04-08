using System.Collections.Generic;
using System.Linq;
using System;

namespace TangibleNode
{
    class StateLog
    {
        public NodePeers Peers {get;} = new NodePeers();
        public PQueue PriorityQueue {get;} = new PQueue(); 
        
        // action id, point ids
        private TDict<string, List<string>> _currentTasks {get;} = new TDict<string, List<string>>();
        private TDict<string, TDict<string, Request>> BatchesBehind {get;} = new TDict<string, TDict<string, Request>>();
        private TDict<string, HashSet<string>> ActionsCompleted {get;} = new TDict<string, HashSet<string>>();
        private TSet<string> MyCompletedActions {get;} = new TSet<string>();
        private readonly object _batch_lock = new object();
        private readonly object _action_lock = new object();
        private readonly object _request_lock = new object();

        public HashSet<string> Leader_GetActionsCompleted(string peerID)
        {
            lock (_action_lock)
            {
                if (!ActionsCompleted.ContainsKey(peerID)) return new HashSet<string>();
                HashSet<string> behind = new HashSet<string>();
                foreach(string s in ActionsCompleted[peerID].ToList())
                {
                    if (!behind.Contains(s))
                        behind.Add(s);
                }
                return behind;
            }
        }

        public int Leader_GetActionsCompletedCount(string peerID)
        {
            lock (_action_lock)
            {
                if (!ActionsCompleted.ContainsKey(peerID)) return 0;
                return ActionsCompleted[peerID].Count;
            }
        }

        public void Leader_RemoveActionsCompleted(string peerID, string action)
        {
            lock (_action_lock)
            {
                if (ActionsCompleted.ContainsKey(peerID))
                {
                    if (ActionsCompleted[peerID].Contains(action)) ActionsCompleted[peerID].Remove(action);
                    // Logger.Write(Logger.Tag.INFO, "Removed " + action.Substring(0,10) +"... from " + peerID);
                } else ActionsCompleted.Add(peerID, new HashSet<string>());
            }
        }

        public void Leader_AddActionCompleted(string actionID)
        {
            lock(_action_lock)
            {
                Peers.ForEachPeer((p) => {
                    p.RemoveAction(actionID);
                    if (!this.ActionsCompleted.ContainsKey(p.Client.ID))
                        this.ActionsCompleted.Add(p.Client.ID, new HashSet<string>());
                    if (!this.ActionsCompleted[p.Client.ID].Contains(actionID))
                    {
                        this.ActionsCompleted[p.Client.ID].Add(actionID);
                        Logger.Write(Logger.Tag.COMMIT, "Comitted [action:" + actionID.Substring(0,10)+"...] COMPLETE, to " + p.Client.ID + ", behind: " + this.ActionsCompleted[p.Client.ID].Count);
                    }
                });
            }
        }

        public bool NotAnyBatchOrCompleteBehind()
        {
            // lock(_action_lock) lock(_batch_lock) lock(_request_lock)
            // {
                bool ready = true;
                foreach (Node n in Peers.AsNodes)
                {
                    if (BatchesBehindCount(n.ID) != 0 || Leader_GetActionsCompletedCount(n.ID) != 0)
                    {
                        ready = false;
                        break;
                    } 
                }
                return ready;
            // }
        }

        public void Follower_AddActionCompleted(string actionID)
        {
            lock(_action_lock)
            {
                MyCompletedActions.Remove(actionID);
                Peers.ForEachPeer((p) => {
                    p.RemoveAction(actionID);
                });
                Logger.Write(Logger.Tag.COMMIT, "Commited COMPLETION [action:" + actionID.Substring(0,10)+"...]");
            }
        }

        public void Follower_MarkActionCompleted(string actionID)
        {
            lock (_action_lock)
                MyCompletedActions.Add(actionID);
        }

        public List<string> Follower_GetCompletedActions()
        {
            lock (_action_lock)
            {
                List<string> completed = new List<string>();
                foreach(string s in MyCompletedActions.ToList())
                {
                    completed.Add(s);
                    if (completed.Count >= Params.BATCH_SIZE) break;
                }
                return completed;
                // return  MyCompletedActions.ToList();
            }
        }

        public List<Request> GetBatchesBehind(string peerID)
        {
            lock (_batch_lock)
            {
                if (!BatchesBehind.ContainsKey(peerID)) return new List<Request>();
                List<Request> behind = new List<Request>();
                foreach (Request r in BatchesBehind[peerID].Values.ToList())
                {
                    behind.Add(r);
                    if (behind.Count > Params.BATCH_SIZE) break;
                }
                return behind;
            }
        }

        public int BatchesBehindCount(string peerID)
        {
            lock (_batch_lock)
            {
                if (!BatchesBehind.ContainsKey(peerID)) return 0;
                return BatchesBehind[peerID].Values.Count;
            }
        }

        public void RemoveBatchBehind(string peerID, Request request)
        {
            lock(_batch_lock)
            {
                if (BatchesBehind.ContainsKey(peerID))
                {
                    BatchesBehind[peerID].Remove(request.ID);
                } 
            }
        }

        public void ClearPeerLog(string peerID)
        {
            lock(_batch_lock)
            {
                if (BatchesBehind.ContainsKey(peerID))
                {
                    BatchesBehind.Remove(peerID);
                } 
            }
            lock(_action_lock)
            {
                if (ActionsCompleted.ContainsKey(peerID))
                {
                    ActionsCompleted.Remove(peerID);
                }
            }
        }

        public void AddRequestBehind(string peerID, Request request)
        {
            lock(_batch_lock)
            {
                if (!BatchesBehind.ContainsKey(peerID))
                    BatchesBehind.Add(peerID, new TDict<string, Request>());
                if (!BatchesBehind[peerID].ContainsKey(request.ID))
                    BatchesBehind[peerID].Add(request.ID, request);
            }
        }

        public void AddRequestBehindToAllBut(string peerID, Request request)
        {
            lock(_request_lock)
            {
                Peers.ForEachPeer((p)=>{
                    if (p.Client.ID != peerID)
                    {
                        AddRequestBehind(p.Client.ID, request);
                    }
                });
            }
        }
        public void AddRequestBehindToAll(Request request)
        {
            lock(_request_lock)
            {
                Peers.ForEachPeer((p)=>{
                    AddRequestBehind(p.Client.ID, request);
                });
            }
        }

        public void RemoveCurrentTask(string actionID)
        {
            lock (_task_lock)
            {
                if (_currentTasks.ContainsKey(actionID))
                {
                    _currentTasks.Remove(actionID);
                }
            }
        }

        public void AppendAction(Action action)
        {
            lock (_action_lock)
            {
                if (action.Assigned == Params.ID)
                {
                    if (!_currentTasks.ContainsKey(action.ID))
                        _currentTasks.Add(action.ID, action.PointID); 
                    else _currentTasks[action.ID].AddRange(action.PointID);
                    PriorityQueue.Enqueue(action);
                } else 
                {
                    Peers.AppendAction(action.Assigned, action);
                }
            }
        }

        public int ActionCount
        {
            get 
            {
                lock (_task_lock)
                {
                    int i0 = 0;
                    _currentTasks.Values.ToList().ForEach((l) => {
                        if (l!=null) i0 += l.Count;
                    });
                    return i0;
                }
            }
        }

        public int LogCount 
        {
            get 
            {
                lock(_task_lock)
                {
                    int i0 = 0;
                    _currentTasks.Values.ToList().ForEach((l) => {
                        if (l!=null) i0 += l.Count;
                    });
                    int i1 = Peers.PeerLogCount;
                    return i0+i1;
                }
            }
        }

        private static readonly object _lock = new object();
        private readonly object _task_lock = new object();
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