using System.Linq;
using System.Collections.Generic;

namespace TangibleDriver
{
    public class BaseHandler 
    {
        public TQueue<PointRequest> Requests = new TQueue<PointRequest>();
        private TDict<string, Request> _requestsBehind {get;} = new TDict<string, Request>();
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
                    }
                } else 
                {
                    lock(_lock)
                    {
                        _requestsBehind.Add(r.ID, r);
                    }
                }
            });
        }

        public void Start(IRequestHandler handler)
        {
            while (true)
            {
                if (Requests.Count > 0)
                {
                    PointRequest request = Requests.Dequeue();
                    List<ValueResponse> response = handler.OnRequest(request);

                    List<Request> batches;
                    lock(_lock)
                    {
                        batches = _requestsBehind.Values.ToList();
                    }
                    response.ForEach((rs) => {
                        batches.Add(
                            new Request(){
                                ID = Utils.GenerateUUID(),
                                Type = Request._Type.POINT,
                                Data = Encoder.EncodeValueResponse(rs)
                            }
                        );
                    });

                    _client.StartClient(new RequestBatch(){
                        Batch = batches,
                        Sender = null,
                        Step = 0,
                        Completed = null
                    }, this);
                }
            }
        }
    }
}