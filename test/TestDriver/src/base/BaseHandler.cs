using System.Linq;
using System.Collections.Generic;
using System;

namespace TangibleDriver
{
    public class BaseHandler 
    {
        public TQueue<PointRequest> Requests = new TQueue<PointRequest>();
        private TDict<string, Request> _requestsBehind {get;} = new TDict<string, Request>();
        private TSet<string> _currentlySending {get;} = new TSet<string>();
        private SynchronousClient _client {get;} = new SynchronousClient();
        private object _lock {get;} = new object();

        public void OnResponse(RequestBatch batch, Response response)
        {
            batch.Batch.ForEach((r) => {
                if (response != null && response.Status != null && response.Status.ContainsKey(r.ID) && response.Status[r.ID])
                {
                    lock (_lock)
                    {
                        _requestsBehind.Remove(r.ID);
                        Console.WriteLine("Removed " + r.ID);
                    }
                } else 
                {
                    lock(_lock)
                    {
                        _requestsBehind.Add(r.ID, r);
                    }
                }
                if (_currentlySending.Contains(r.ID))
                    _currentlySending.Remove(r.ID);
            });
        }

        public void Start(IRequestHandler handler)
        {
            while (true)
            {
                try 
                {
                    if (Requests.Count > 1)
                    {

                        List<Request> batches = new List<Request>();
                        // for (int k = 0; k < 2; k++)
                        // {
                            PointRequest request = Requests.Dequeue();
                            if (request != null)
                            {
                                ValueResponse response = handler.OnRequest(request);
                                Request r0 = new Request(){
                                        ID = Utils.GenerateUUID(),
                                        Type = Request._Type.POINT,
                                        Data = Encoder.EncodeValueResponse(response)
                                    };
                                batches.Add(r0);
                            }
                        // }
                        // int s = (Params.BATCH_SIZE-10)/response.Message.Count;
                        // if (s > 5) s = 5;
                        // Console.WriteLine("max size: " + s);
                        foreach(Request r in _requestsBehind.Values.ToList())
                        {
                            lock(_lock)
                            {
                                if (!_currentlySending.Contains(r.ID) && batches.Count < 1)
                                {
                                    batches.Add(r);
                                    _currentlySending.Add(r.ID);
                                }
                                // if (batches.Count >= s) break;
                            }   
                        }

                        // if (batches.Count < s)
                        // {
                        // response.Timestamp = Utils.Micros.ToString();
                        // for (int i = 0; i< 2; i++)
                        // {
                            // if (request != null)
                            // {
                        //     }
                        // }
                        // } else 
                        // {
                        //     Requests.Enqueue(request);
                        // }
                        RequestBatch rb = new RequestBatch(){
                            Batch = batches,
                            Sender = null,
                            Step = 0,
                            Completed = null
                        };
                        // Console.WriteLine("RB length: " + Encoder.EncodeRequestBatch(rb).Length);
                        _client.StartClient(rb, this);
                        Logger.Write(Logger.Tag.INFO, "Returned batch of " + batches.Count + ", behind: " + _requestsBehind.Count);
                    }
                } catch (Exception e)
                {   
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }
        }
    }
}