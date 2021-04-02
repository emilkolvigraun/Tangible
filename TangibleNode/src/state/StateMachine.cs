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
            Response r0 = Encoder.DecodeResponse(response);
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
                                LogCount = StateLog.Instance.Peers.LogCount
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
                            
                            StateLog.Instance.ClearPeerLog(p.Client.ID);
                            StateLog.Instance.Peers.TryRemoveNode(p.Client.ID);
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

                // Logger.WriteState();
            }
        }   

    }
}