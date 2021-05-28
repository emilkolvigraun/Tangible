using System.Linq;
using System.Collections.Generic;
using System;

namespace TangibleDriver
{
    public class MainLoop 
    {
        public TQueue<DataRequest> Requests = new TQueue<DataRequest>();
        private TDict<string, Call> _requestsBehind {get;} = new TDict<string, Call>();
        private TSet<string> _currentlySending {get;} = new TSet<string>();
        private SynchronousClient _client {get;} = new SynchronousClient();
        private object _lock {get;} = new object();

        public void OnResponse(ProcedureCallBatch batch, Response response)
        {
            batch.Batch.ForEach((r) => {
                if (response != null && response.Status != null && response.Status.ContainsKey(r.ID) && response.Status[r.ID])
                {
                    lock (_lock)
                    {
                        _requestsBehind.Remove(r.ID);
                        Logger.Write(Logger.Tag.INFO, "Successfully transmitted: " + r.ID);
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
                    if (Requests.Count > 0 || _requestsBehind.Count > 0)
                    {
                        DataRequest request = Requests.Dequeue();
                        // Console.WriteLine(Encoder.SerializeObject(request));
                        if (request != null)
                        {
                            if (request.Type == DataRequest._Type.WRITE)
                            {
                                WriteRequest wr = WriteRequest.Parse(request);
                                if(wr!=null) handler.ProcessWrite(wr);
                            }
                        }

                        AddRangeAsCalls(handler.GetResponses());
                        List<Call> batches = new List<Call>();
                        foreach(Call r in _requestsBehind.Values.ToList())
                        {
                            lock(_lock)
                            {
                                if (!_currentlySending.Contains(r.ID) && batches.Count < 10)
                                {
                                    batches.Add(r);
                                    _currentlySending.Add(r.ID);
                                } 
                                if (batches.Count >= 10) break;
                            }   
                        }

                        if (batches.Count > 0)
                        {
                            ProcedureCallBatch rb = new ProcedureCallBatch(){
                                Batch = batches,
                                Sender = null,
                                Completed = null
                            };
                            _client.StartClient(rb, this);
                            Logger.Write(Logger.Tag.INFO, "Behind: " + _requestsBehind.Count);
                        }
                    }
                } catch (Exception e)
                {   
                    Logger.Write(Logger.Tag.ERROR, e.ToString());
                    continue;
                }
            }
        }

        private void AddRangeAsCalls(ValueResponseBatch[] batches)
        {
            foreach(ValueResponseBatch batch in batches)
            {
                string id = Utils.GenerateUUID();
                bool b = false;
                lock(_lock)
                {
                    b = _requestsBehind.ContainsKey(id);
                }
                while (b)
                {
                    lock(_lock)
                    {
                        id = Utils.GenerateUUID();
                        b = _requestsBehind.ContainsKey(id);
                    }
                }
                lock(_lock)
                {
                    _requestsBehind.Add(
                        id,
                        new Call{
                            ID = id,
                            Type = Call._Type.VALUE_RESPONSE,
                            Data = Encoder.EncodeValueResponseBatch(batch)
                        }
                    );
                    Logger.Write(Logger.Tag.INFO, "Appended [" + id.Substring(0,10) + "] to transmit batch.");
                }
            }
        }
    }
}