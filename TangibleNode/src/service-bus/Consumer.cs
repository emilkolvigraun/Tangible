using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace TangibleNode
{
    class Consumer 
    {
        private HardwareAbstraction HA {get;}
        private bool _running {get; set;} = false;

        private bool _ready {get; set;} = false;
        private readonly object _ready_lock = new object();

        public Consumer (HardwareAbstraction HA)
        {
            this.HA = HA;
        }

        int amount = 0;
        // int interval = 100;
        // long t0 = Utils.Millis+500;
        public void MarkReady(bool b)
        {
            lock(_ready_lock)
            { 
                _ready = b;
                // if (Utils.Millis > t0 && _ready && amount < 10)
                if (_ready && amount < 1000001)
                {
                    // if (IsReady && amount < 2000000)
                    // {
                        amount++;
                        Params.STEP++;
                        ReceiveDataRequest(new DataRequest(){
                            Type = Action._Type.WRITE,
                            Benv = new Location(){
                                HasPoint = new List<Point>{
                                    new Point(){ID = "1abc"}
                                    // new Point(){ID = "2abc"}
                                    }
                            },
                            Priority = 2,
                            Value = Params.STEP.ToString(),
                            T0 = Utils.Micros.ToString(),
                            ReturnTopic = "MyApplication"
                        });
                        // interval = ((int)((1m/Params.HERTZ)*1000m));
                        // t0 = Utils.Millis+500;

                        // if (!leader)
                        // {
                        //     t_die_l = Utils.Millis+Params.DIE_AS_LEADER;
                        //     leader=true;
                        // }
                        // if (Params.DIE_AS_LEADER!=-1&&t2>=t_die_l) Environment.Exit(0);
                        // if (amount >= 1000) 
                        // {
                        //     if (Params.HERTZ==1m) Params.HERTZ += 9m;
                        //     else Params.HERTZ+=10m;
                        //     interval = ((int)((1m/Params.HERTZ)*1000m));
                        //     amount = 0;
                        // } if (Params.HERTZ > 2000) Environment.Exit(0);
                        MarkReady(false);
                    // }
                }
                // if (!_ready) t0 = Utils.Millis-(Utils.Millis-t2);
            }
        }

        public bool IsReady
        {
            get 
            {
                lock(_ready_lock)
                {
                    return _ready;
                }
            }
        }

        public void Handle(ESBRequest request)
        {
            if (request.Type == ESBRequest._Type.ACTION)
                ReceiveDataRequest(Encoder.DecodeDataRequest(request.Data));
            else if (request.Type == ESBRequest._Type.BROADCAST)
                ReceiveBroadcast(Encoder.DecodeBroadcast(request.Data));
        }

        // long t0 = 0;
        // long t1 = 0;
        // long t2 = 0;
        private readonly object _t_lock = new object();
        public void Start()
        {
            // Utils.Sleep(200);
            // int interval = ((int)((1m/Params.HERTZ)*1000m));
            
            // lock(_t_lock)
            // {
                // t0 = Utils.Millis;
            // }
            // Logger.Write(Logger.Tag.INFO, "Started consuming from ESB on " + Params.REQUEST_TOPIC + " and " + Params.BROADCAST_TOPIC +".");
            
            // if (CurrentState.Instance.Get_State.State != State.LEADER) Utils.Sleep(Params.WAIT_BEFORE_START);
            // long t_die_f = Utils.Millis+Params.DIE_AS_FOLLOWER;
            // long t_die_l = Utils.Millis;
            // bool leader = false;
            // int amount = 0;
            // while (true)
            // {
            //     lock(_t_lock)
            //     {
            //         // t1 = Utils.Millis - t0;
            //         // t2 = Utils.Millis;
            //         // bool timePassed = t1>=interval;
            //         // if (timePassed && CurrentState.Instance.IsLeader && IsReady)
            //         if (IsReady && amount < 2000000)
            //         {
            //             amount++;
            //             Params.STEP++;
            //             ReceiveDataRequest(new DataRequest(){
            //                 Type = Action._Type.WRITE,
            //                 Benv = new Location(){
            //                     HasPoint = new List<Point>{new Point(){ID = "123abc"}}
            //                 },
            //                 Priority = 2,
            //                 Value = Params.STEP.ToString(),
            //                 T0 = Utils.Millis.ToString(),
            //                 ReturnTopic = "MyApplication"
            //             });
            //             // if (!leader)
            //             // {
            //             //     t_die_l = Utils.Millis+Params.DIE_AS_LEADER;
            //             //     leader=true;
            //             // }
            //             // if (Params.DIE_AS_LEADER!=-1&&t2>=t_die_l) Environment.Exit(0);
            //             // if (amount >= 1000) 
            //             // {
            //             //     if (Params.HERTZ==1m) Params.HERTZ += 9m;
            //             //     else Params.HERTZ+=10m;
            //             //     interval = ((int)((1m/Params.HERTZ)*1000m));
            //             //     amount = 0;
            //             // } if (Params.HERTZ > 2000) Environment.Exit(0);
            //             MarkReady(false);
            //         }
            //     }
            //     // else if (!CurrentState.Instance.IsLeader)
            //     // {
            //     //     t0 = Utils.Millis-(Utils.Millis-t2);
            //     //     if (Params.DIE_AS_FOLLOWER!=-1&&t2>=t_die_f) Environment.Exit(0);
            //     // }
            // }
        }

        public void ReceiveDataRequest(DataRequest dataRequest)
        {
            // List<Request> requests = HA.MarshallDataRequest(dataRequest);
            HA.MarshallDataRequest(dataRequest);
        }

        public void ReceiveBroadcast(Broadcast broadcast)
        {
            if (broadcast.Self.ID == Params.ID ||StateLog.Instance.Peers.ContainsPeer(broadcast.Self.ID)) return; 
            Logger.Write(Logger.Tag.INFO,"Received broadcast from [node:"+broadcast.Self.ID+"]");
            StateLog.Instance.Peers.AddIfNew(broadcast.Self);

            Request request = new Request()
            {
                ID = Utils.GenerateUUID(),
                Data = Encoder.EncodeNode(broadcast.Self),
                Type = Request._Type.NODE_ADD
            };

            Task[] tasks;
            StateLog.Instance.Peers.ForEachAsync((p0) => {
                RequestBatch rb = new RequestBatch(){
                    Batch = new List<Request>{request},
                    Completed = StateLog.Instance.Leader_GetActionsCompleted(p0.Client.ID),
                    Sender = Node.Self
                };
                rb.Batch.AddRange(StateLog.Instance.GetBatchesBehind(p0.Client.ID));
                
                StateLog.Instance.Peers.ForEachPeer((p1) => {
                    if (p1.Client.ID != p0.Client.ID)
                    {
                        rb.Batch.Add( new Request(){
                            ID = Utils.GenerateUUID(),
                            Data = Encoder.EncodeNode(p1.AsNode),
                            Type = Request._Type.NODE_ADD
                        });
                    }
                });

                p0.Client.StartClient(rb, new DefaultHandler());
            }, out tasks);
            
            Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
            Task.WaitAll(tasks);
        }
    }
}