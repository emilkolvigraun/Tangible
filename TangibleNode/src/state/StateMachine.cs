using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace TangibleNode 
{

    class VoteResponseHandler : IResponseHandler
    {
        private object _lock1 {get;} = new object();
        private object _lock2 {get;} = new object();
        private List<string> votes {get;} = new List<string>();
        private int _quorum {get; set;} = 0;
        public int Quorum {
            get {
                lock(_lock1)
                {
                    return _quorum;
                }
            } 
            set {
                lock(_lock1)
                {
                    _quorum = value;
                }
            }
        }
        public void OnResponse(string receiverID, RequestBatch sender, string response)
        {
            if (response==null) return;
            Response r0 = Encoder.DecodeResponse(response);
            if (r0==null||r0.Status==null)
            {
                // Will never be leader (well, not until next term)
                return;
            }
            sender.Batch.ForEach((r) => {
                if (r0.Status.ContainsKey(r.ID))
                {
                    if (r0.Status[r.ID])
                    {
                        Vote v0 = Encoder.DecodeVote(r0.Data);
                        lock(_lock2)
                        {
                            votes.Add(v0.ID);
                        }
                    } else 
                    {
                        lock(_lock2)
                        {
                            votes.Add(Params.ID);
                        }
                    }
                }
            });
            lock(_lock2)
            {
                if (votes.Count == Quorum && !votes.Any((s) => s != Params.ID) && Utils.IsCandidate(CurrentState.Instance.Get_State.State))
                {
                    CurrentState.Instance.SetStateAsLeader();

                } else if (votes.Count == Quorum) 
                {
                    CurrentState.Instance.CancelState();
                    CurrentState.Instance.Timer.Reset(((int)(Params.HEARTBEAT_MS/2)));
                }
            }
        }
    }

    class StateMachine 
    {
        private Consumer _consumer {get;}
        private TTimer _sleepingTimer {get;}

        public StateMachine(Consumer consumer)
        {
            _consumer = consumer;
            _sleepingTimer = new TTimer("sleepingTimer");
        }

        public void Start(List<Node> tcpNodes = default(List<Node>))
        {
            // Giving the server time to start, so that it is able to receive connections
            Utils.Sleep(200);

            foreach (Node node in tcpNodes)
                _consumer.ReceiveBroadcast(
                    new Broadcast()
                    {
                        Self = node 
                    }
                );

            _sleepingTimer.Begin();

            while (true)
            {
                bool morePeers = (StateLog.Instance.Peers.NodeCount > 0);
                bool timerActive = CurrentState.Instance.Timer.Active;
                (State State, bool TimePassed) state = CurrentState.Instance.Get_State;
                
                if (!morePeers && timerActive) 
                {
                    CurrentState.Instance.Timer.End();
                    _sleepingTimer.Begin();
                } else if(morePeers && !timerActive) 
                {
                    CurrentState.Instance.Timer.Begin();
                    _sleepingTimer.End();
                }

                if (Utils.IsCandidate(state.State))
                {
                    List<Request> batch = new List<Request>{
                        new Request(){
                            Type = Request._Type.VOTE,
                            ID = Utils.GenerateUUID(),
                            Data = Encoder.EncodeVote(new Vote(){
                                ID = Params.ID,
                                LogCount = StateLog.Instance.LogCount
                            })
                        }
                    };
                    VoteResponseHandler handler = new VoteResponseHandler()
                    {
                        Quorum = StateLog.Instance.Peers.NodeCount
                    };
                    Task[] tasks;
                    StateLog.Instance.Peers.ForEachAsync((p) => {
                        RequestBatch rb = new RequestBatch(){
                            Batch = batch,
                            Sender = Node.Self
                        };
                        p.Client.StartClient(rb, handler);
                    }, out tasks);
                    Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
                    Task.WaitAll(tasks);
                }

                if (CurrentState.Instance.IsLeader)
                {
                    bool connectionsLost = false;
                    StateLog.Instance.Peers.ForEachPeer((p) => {
                        if (p.Heartbeat.Value >= Params.MAX_RETRIES)
                        {
                            connectionsLost = true;
                            StateLog.Instance.AddRequestBehindToAllBut(p.Client.ID, new Request(){
                                Type = Request._Type.NODE_DEL,
                                ID = Utils.GenerateUUID(),
                                Data = Encoder.EncodeNode(p.AsNode)
                            });

                            Peer peer = null;
                            bool success = StateLog.Instance.Peers.TryGetNode(p.Client.ID, out peer);

                            if (success)
                            {
                                HashSet<string> _rescheduledActions = new HashSet<string>();
                                peer.ForEachAction((a)=>{
                                    if (!_rescheduledActions.Contains(a.ID))
                                    {
                                        _rescheduledActions.Add(a.ID);
                                        Action action = a;
                                        action.Assigned = StateLog.Instance.Peers.ScheduleAction(p.Client.ID);
                                        action.ID = Utils.GenerateUUID();
                                        StateLog.Instance.AddRequestBehindToAllBut(p.Client.ID, new Request(){
                                            Type = Request._Type.ACTION,
                                            ID = Utils.GenerateUUID(),
                                            Data = Encoder.EncodeAction(action)
                                        });
                                        StateLog.Instance.AppendAction(action);
                                    }
                                });
                                // StateLog.Instance.GetBatchesBehind(p.Client.ID).ForEach((b) => {
                                //     if (b.Type == Request._Type.ACTION)
                                //     {
                                //         Action a0 = Encoder.DecodeAction(b.Data);
                                //         Console.WriteLine(p.Client.ID + " WAS BEHIND" + a0.Value);
                                //         if (!_rescheduledActions.Contains(a0.ID) && a0.Assigned == p.Client.ID)
                                //         {
                                //             _rescheduledActions.Add(a0.ID);
                                //             Action action = a0;
                                //             action.Assigned = StateLog.Instance.Peers.ScheduleAction(p.Client.ID);
                                //             action.ID = Utils.GenerateUUID();
                                //             StateLog.Instance.AddRequestBehindToAllBut(p.Client.ID, new Request(){
                                //                 Type = Request._Type.ACTION,
                                //                 ID = Utils.GenerateUUID(),
                                //                 Data = Encoder.EncodeAction(action)
                                //             });
                                //             StateLog.Instance.AppendAction(action);
                                //         }
                                //     }
                                // });
                            }
                            
                            StateLog.Instance.ClearPeerLog(p.Client.ID);
                            StateLog.Instance.Peers.TryRemoveNode(p.Client.ID);
                        } else 
                        {
                            connectionsLost = StateLog.Instance.GetBatchesBehind(p.Client.ID).Count > 0;
                        }
                    });

                    if(CurrentState.Instance.Timer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2))) || connectionsLost)
                    {
                        Task[] tasks;
                        StateLog.Instance.Peers.ForEachAsync((p) => {
                            RequestBatch rb = new RequestBatch(){
                                Batch = StateLog.Instance.GetBatchesBehind(p.Client.ID),
                                Completed = StateLog.Instance.Leader_GetActionsCompleted(p.Client.ID),
                                Sender = Node.Self
                            };
                            p.Client.StartClient(rb, new DefaultHandler());
                        }, out tasks);
                        Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
                        Task.WaitAll(tasks);
                        CurrentState.Instance.Timer.Reset();
                        Producer.Instance.Broadcast();
                    } 
                }

                if (state.State == State.SLEEPER && _sleepingTimer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2))))
                {
                    Producer.Instance.Broadcast();
                    _sleepingTimer.Reset();
                }

                // Action prioritizedAction = StateLog.Instance.PriorityQueue.Dequeue();

                // Logger.WriteState();
            }
        }   

    }
}