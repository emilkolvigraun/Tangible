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
        public void OnResponse(string receiverID, ProcedureCallBatch sender, string response)
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
                            votes.Add("*");
                        }
                    }
                }
            });
            lock(_lock2)
            {
                // foreach(string s in votes) Console.WriteLine(s);
                // if (CurrentState.Instance.ReceviedVote)
                // {
                //     CurrentState.Instance.CancelState();
                //     CurrentState.Instance.Timer.Reset(((int)(Params.HEARTBEAT_MS/2)));
                //     StateMachine.SetElectionTerm(false);
                // }
                
                // if (votes.Count == Quorum && votes.Any((s) => s != Params.ID))
                HashSet<string> _votes = new HashSet<string>(votes);
                if (votes.Count == Quorum && _votes.Count == 1 && votes.Contains("*") && !CurrentState.Instance.CandidateResolve)
                {
                    CurrentState.Instance.ActAsSleeper();
                    CurrentState.Instance.SetCandidateResolve(true);
                }
                else if (votes.Count == Quorum && _votes.Count > 0 && votes.Contains(Params.ID) && Utils.IsCandidate(CurrentState.Instance.Get_State.State))
                {
                    CurrentState.Instance.SetStateAsLeader();
                    CurrentState.Instance.SetCandidateResolve(false);
                } 
                else if (votes.Count == Quorum && !CurrentState.Instance.CandidateResolve) 
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

        public void Start(List<Credentials> tcpNodes = default(List<Credentials>))
        {
            // Giving the server time to start, so that it is able to receive connections
            Utils.Sleep(200);

            foreach (Credentials node in tcpNodes)
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
            bool morePeers = (StateLog.Instance.Nodes.NodeCount > 0);
            bool timerActive = CurrentState.Instance.Timer.Active;
            (State State, bool TimePassed) state = CurrentState.Instance.Get_State;
            
            if (!morePeers && timerActive) 
            {
                CurrentState.Instance.Timer.End();
                _sleepingTimer.Begin();
                CurrentState.Instance.ActAsSleeper();
                // Logger.Write(Logger.Tag.WARN, state.State.ToString());
            } else if(morePeers && !timerActive) 
            {
                CurrentState.Instance.Timer.Begin();
                _sleepingTimer.End();
            }

            if (Utils.IsCandidate(state.State) || (CurrentState.Instance.CandidateResolve && state.State == State.SLEEPER)) // && !ElectionTerm)
            {
                // CurrentState.Instance.SetReceivedVote(false);
                List<Call> batch = new List<Call>{
                    new Call(){
                        Type = Call._Type.VOTE,
                        ID = Utils.GenerateUUID(),
                        Data = Encoder.EncodeVote(new Vote(){
                            ID = Params.ID,
                            LogCount = StateLog.Instance.LogCount
                        })
                    }
                };
                VoteResponseHandler handler = new VoteResponseHandler()
                {
                    Quorum = StateLog.Instance.Nodes.NodeCount
                };
                Task[] tasks;
                StateLog.Instance.Nodes.ForEachAsync((p) => {
                    ProcedureCallBatch rb = new ProcedureCallBatch(){
                        Batch = batch,
                        Sender = Credentials.Self
                    };
                    p.Client.StartClient(rb, handler, true);
                }, out tasks);
                Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
                Task.WaitAll(tasks);
                // ElectionTerm = true;
            }

            if (CurrentState.Instance.IsLeader || (CurrentState.Instance.CandidateResolve && state.State == State.SLEEPER))
            {
                bool connectionsLost = false;
                StateLog.Instance.Nodes.ForEachPeer((p) => {
                    if (p.Heartbeat.Value >= Params.MAX_RETRIES)
                    {
                        connectionsLost = true;
                        
                        if (StateLog.Instance.Nodes.NodeCount > 1)
                        {
                            StateLog.Instance.AddBehindToAllButOne(p.Client.ID, new Call(){
                                Type = Call._Type.NODE_DEL,
                                ID = Utils.GenerateUUID(),
                                Data = Encoder.EncodeNode(p.AsNode)
                            });

                        }

                        Node peer = null;
                        bool success = StateLog.Instance.Nodes.TryGetNode(p.Client.ID, out peer);
                        Dictionary<string, DataRequest> _rescheduledActions = new Dictionary<string, DataRequest>();

                        if (success && !CurrentState.Instance.CandidateResolve)
                        {
                            peer.ForEachAction((a)=>{
                                if (!_rescheduledActions.ContainsKey(a.ID))
                                {
                                    DataRequest action = a;
                                    action.Assigned = StateLog.Instance.Nodes.ScheduleRequest(p.Client.ID);
                                    action.ID = Utils.GenerateUUID();
                                    StateLog.Instance.AddBehindToAllButOne(p.Client.ID, new Call(){
                                        Type = Call._Type.DATA_REQUEST,
                                        ID = Utils.GenerateUUID(),
                                        Data = Encoder.EncodeDataRequest(action)
                                    });
                                    _rescheduledActions.Add(a.ID, action);
                                }
                            });
                        } else if (success && CurrentState.Instance.CandidateResolve)
                        {
                            peer.ForEachAction((a)=>{
                                if (!_rescheduledActions.ContainsKey(a.ID))
                                {
                                    DataRequest action = a;
                                    action.Assigned = Params.ID;
                                    action.ID = Utils.GenerateUUID();
                                    StateLog.Instance.AddBehindToAllButOne(p.Client.ID, new Call(){
                                        Type = Call._Type.DATA_REQUEST,
                                        ID = Utils.GenerateUUID(),
                                        Data = Encoder.EncodeDataRequest(action)
                                    });
                                    _rescheduledActions.Add(a.ID, action);
                                }
                            });
                        }
                        
                        StateLog.Instance.ClearPeerLog(p.Client.ID);
                        StateLog.Instance.Nodes.TryRemoveNode(p.Client.ID);

                        if (StateLog.Instance.Nodes.NodeCount < 1) CurrentState.Instance.SetCandidateResolve(false);
                        
                        foreach(var action in _rescheduledActions.Values)
                        {
                            StateLog.Instance.AppendAction(action);
                        }
                    } else 
                    {
                        connectionsLost = StateLog.Instance.BatchesBehindCount(p.Client.ID) > 0;
                    }
                });

                if (CurrentState.Instance.IsLeader)
                {
                    bool ready = StateLog.Instance.NotAnyBatchOrCompleteBehind();
                    bool passed = CurrentState.Instance.Timer.HasTimePassed(((int)(Params.HEARTBEAT_MS/2)));

                    if (!ready || passed || connectionsLost || StateLog.Instance.Nodes.PeerLogCount > StateLog.Instance.Nodes.NodeCount)
                    {
                        Task[] tasks;
                        StateLog.Instance.Nodes.ForEachAsync((p) => {
                            HashSet<string> acp = StateLog.Instance.Leader_GetActionsCompleted(p.Client.ID);
                            ProcedureCallBatch rb = new ProcedureCallBatch(){
                                Batch = StateLog.Instance.GetBatchesBehind(p.Client.ID),
                                Completed = acp,
                                Sender = Credentials.Self
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

                    if (wait < Utils.Millis)
                    {
                        ready = StateLog.Instance.NotAnyBatchOrCompleteBehind();
                        _consumer.MarkReady(ready);
                    }
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
            if (state.State == State.SLEEPER)
            {
                _consumer.MarkReady(true);
            }

            if(Params.RUN_HIE)
            {
                int balance =  StateLog.Instance.Nodes.NodeCount*2;
                if (balance < 1) balance = 1;
                for (int bs = 0; bs < balance; bs++)
                {
                    DataRequest prioritizedAction = StateLog.Instance.PriorityQueue.Dequeue();
                    if (prioritizedAction != null)
                    {
                        List<Driver> requiredDriver = HIE.GetOrCreateDriver(prioritizedAction);
                        requiredDriver.ForEach((dr0) => {dr0.AddRequestBehind(prioritizedAction);});
                    }
                }
                List<Task> tasks = new List<Task>();
                HIE.ForEachDriver((d) => {
                    Task t = new Task(
                        ()=>{
                            d.Write();
                        }
                    );
                    tasks.Add(t);
                });
                Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
                Task ta = Task.WhenAll(tasks);
                try {
                    ta.Wait();
                }
                catch {} 
                TestReceiverClient.Instance.StartClient();
            }

            // if (FileLogger.Instance.IsEnabled && Utils.Millis > FileLogTS+1000 && !CurrentState.Instance.IsLeader)
            // {
            //     long f = Utils.Millis;
            //     int i0 = StateLog.Instance.ActionCount;
            //     int lc = StateLog.Instance.LogCount;
            //     int i1 = StateLog.Instance.PriorityQueue.Count;
            //     int i2 = StateLog.Instance.Peers.NodeCount;
            //     string ram = Utils.MemoryUsage.ToString();
            //     string cpu = Utils.CPUUsage.ToString();
            //     FileLogger.Instance.WriteToFile(
            //         string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
            //         f.ToString(), i0.ToString(), lc.ToString(), i1.ToString(), i2.ToString(), state.State.ToString(), cpu, ram)
            //     );
            //     FileLogTS = Utils.Millis;
            // }
        }
    }
}