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
        private HardwareInteractionEnvironment HIE {get;}
        private long FileLogTS {get; set;} = Utils.Millis;

        public StateMachine(Consumer consumer, HardwareInteractionEnvironment hie)
        {
            _consumer = consumer;
            HIE = hie;
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
                try 
                {
                    Loop();
                } catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        
        long wait = Utils.Millis+Params.WAIT_BEFORE_START;
        private void Loop()
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
                        Dictionary<string, Action> _rescheduledActions = new Dictionary<string, Action>();

                        if (success)
                        {
                            peer.ForEachAction((a)=>{
                                if (!_rescheduledActions.ContainsKey(a.ID))
                                {
                                    Action action = a;
                                    action.Assigned = StateLog.Instance.Peers.ScheduleAction(p.Client.ID);
                                    action.ID = Utils.GenerateUUID();
                                    StateLog.Instance.AddRequestBehindToAllBut(p.Client.ID, new Request(){
                                        Type = Request._Type.ACTION,
                                        ID = Utils.GenerateUUID(),
                                        Data = Encoder.EncodeAction(action)
                                    });
                                    _rescheduledActions.Add(a.ID, action);
                                }
                            });
                        }
                        
                        StateLog.Instance.ClearPeerLog(p.Client.ID);
                        StateLog.Instance.Peers.TryRemoveNode(p.Client.ID);
                        
                        foreach(var action in _rescheduledActions.Values)
                        {
                            StateLog.Instance.AppendAction(action);
                        }
                    } else 
                    {
                        connectionsLost = StateLog.Instance.BatchesBehindCount(p.Client.ID) > 0;
                    }
                });

                // if(CurrentState.Instance.Timer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2))) || connectionsLost)
                // {

                    // if (CurrentState.Instance.IsLeader)
                    // {
                        // bool ready = true;
                        // foreach (Node n in StateLog.Instance.Peers.AsNodes)
                        // {
                        //     if (StateLog.Instance.BatchesBehindCount(n.ID) != 0 || StateLog.Instance.Leader_GetActionsCompletedCount(n.ID) != 0)
                        //     {
                        //         ready = false;
                        //         break;
                        //     } 
                        // }
                        
                    // }
                bool ready = StateLog.Instance.NotAnyBatchOrCompleteBehind();
                bool passed = CurrentState.Instance.Timer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2)));

                if (!ready || passed || connectionsLost)
                {
                    Task[] tasks;
                    StateLog.Instance.Peers.ForEachAsync((p) => {
                        HashSet<string> acp = StateLog.Instance.Leader_GetActionsCompleted(p.Client.ID);
                        RequestBatch rb = new RequestBatch(){
                            Batch = StateLog.Instance.GetBatchesBehind(p.Client.ID),
                            Completed = acp,
                            Sender = Node.Self
                        };
                        p.Client.StartClient(rb, new DefaultHandler());
                    }, out tasks);
                    Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
                    Task t = Task.WhenAll(tasks);
                    try {
                        t.Wait();
                    }
                    catch {}   
                }

                if (passed)
                {
                    Producer.Instance.Broadcast();
                    CurrentState.Instance.Timer.Reset();
                }
            // } 
                if (wait < Utils.Millis)
                {
                    ready = StateLog.Instance.NotAnyBatchOrCompleteBehind();
                    _consumer.MarkReady(ready);
                }
            }

            if (state.State == State.SLEEPER && _sleepingTimer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2))))
            {
                try 
                {
                    Producer.Instance.Broadcast();
                    _sleepingTimer.Reset();
                } catch {}
            }

            if(Params.RUN_HIE)
            {
                Action prioritizedAction = StateLog.Instance.PriorityQueue.Dequeue();
                if (prioritizedAction != null)
                {
                    Driver requiredDriver = HIE.GetOrCreateDriver(prioritizedAction);
                    requiredDriver.AddRequestBehind(
                        new PointRequest(){
                            ID = prioritizedAction.ID,
                            PointIDs = prioritizedAction.PointID,
                            // ReturnTopic = prioritizedAction.ReturnTopic,
                            // T0 = prioritizedAction.T0,
                            // T1 = prioritizedAction.T1,
                            // T2 = Utils.Micros.ToString(),
                            Type = prioritizedAction.Type,
                            Value = prioritizedAction.Value,
                            ReturnTopic = prioritizedAction.ReturnTopic
                    });
                }
                HIE.ForEachDriver((d) => {
                    d.Write();
                });
                TestReceiverClient.Instance.StartClient();
            }

            if (FileLogger.Instance.IsEnabled && Utils.Millis > FileLogTS+1000)
            {
                long f = Utils.Millis;
                int i0 = StateLog.Instance.ActionCount;
                int lc = StateLog.Instance.LogCount;
                int i1 = StateLog.Instance.PriorityQueue.Count;
                int i2 = StateLog.Instance.Peers.NodeCount;
                (double cpu, double ram) usage = Utils.ResourceUsage;
                FileLogger.Instance.WriteToFile(
                    string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
                    f.ToString(), i0.ToString(), lc.ToString(), i1.ToString(), i2.ToString(), state.State.ToString(), usage.cpu.ToString(), usage.ram.ToString())
                );
                FileLogTS = Utils.Millis;
            }
        }
    }
}