using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

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
                        PointRequest request = Requests.Dequeue();
                        if (request != null)
                        {
                            
                            ValueResponse response = handler.OnRequest(request);
                                            
                            Task.Delay(15).GetAwaiter().GetResult();

                            Request r0 = new Request(){
                                    ID = Utils.GenerateUUID(),
                                    Type = Request._Type.POINT,
                                    Data = Encoder.EncodeValueResponse(response)
                                };
                            batches.Add(r0);
                        }
                        foreach(Request r in _requestsBehind.Values.ToList())
                        {
                            lock(_lock)
                            {
                                if (!_currentlySending.Contains(r.ID) && batches.Count < 1)
                                {
                                    batches.Add(r);
                                    _currentlySending.Add(r.ID);
                                }
                            }   
                        }

                        if (batches.Count > 0)
                        {
                            RequestBatch rb = new RequestBatch(){
                                Batch = batches,
                                Sender = null,
                                Step = 0,
                                Completed = null
                            };
                            _client.StartClient(rb, this);
                            Logger.Write(Logger.Tag.INFO, "Returned batch of " + batches.Count + ", behind: " + _requestsBehind.Count);
                        }
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