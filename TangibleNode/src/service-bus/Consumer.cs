using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace TangibleNode
{
    class Consumer 
    {
        private HardwareAbstraction HA {get;}
        private bool _running {get; set;} = false;
        public Consumer (HardwareAbstraction HA)
        {
            this.HA = HA;
        }

        public void Handle(ESBRequest request)
        {
            if (request.Type == ESBRequest._Type.ACTION)
                ReceiveDataRequest(Encoder.DecodeDataRequest(request.Data));
            else if (request.Type == ESBRequest._Type.BROADCAST)
                ReceiveBroadcast(Encoder.DecodeBroadcast(request.Data));
        }

        public void Start()
        {
            Utils.Sleep(200);
            int hertz = 1;
            int interval = (1/hertz)*1000;
            long t0 = Utils.Millis;
            Logger.Write(Logger.Tag.INFO, "Started consuming from ESB on " + Params.REQUEST_TOPIC + " and " + Params.BROADCAST_TOPIC +".");
            
            if (CurrentState.Instance.Get_State.State != State.LEADER) Utils.Sleep(10000);
            while (true)
            {
                long t1 = Utils.Millis - t0;
                long t2 = Utils.Millis;
                if (t1 >= interval && CurrentState.Instance.IsLeader)
                {
                    Params.STEP++;
                    ReceiveDataRequest(new DataRequest(){
                        Type = Action._Type.WRITE,
                        Benv = new Location(){
                            HasPoint = new List<Point>{new Point(){ID = "123abc"}}
                        },
                        Priority = 2,
                        Value = Params.STEP.ToString(),
                        T0 = Utils.Millis.ToString(),
                        ReturnTopic = "MyApplication"
                    });
                    t0 = Utils.Millis-(Utils.Millis-t2);
                } else if (!CurrentState.Instance.IsLeader)
                    t0 = Utils.Millis-(Utils.Millis-t2);
            }
        }

        public void ReceiveDataRequest(DataRequest dataRequest)
        {
            List<Request> requests = HA.MarshallDataRequest(dataRequest);

            Task[] tasks;
            StateLog.Instance.Peers.ForEachAsync((p) => {
                RequestBatch rb = new RequestBatch(){
                    Batch = requests,
                    Completed = StateLog.Instance.Leader_GetActionsCompleted(p.Client.ID),
                    Sender = Node.Self
                };
                p.Client.StartClient(rb, new DefaultHandler());
            }, out tasks);
            Parallel.ForEach<Task>(tasks, (t) => { t.Start(); });
            Task.WaitAll(tasks);
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