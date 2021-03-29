using System.Collections.Generic;
using System.Threading.Tasks;

namespace Node 
{
    class ResponseQueue
    {
        private List<(string, string)> _responses = new List<(string, string)>();

        private static readonly object _lock = new object();
        private static ResponseQueue _instance = null;
        public static ResponseQueue Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if(_instance==null)_instance=new ResponseQueue();
                    return _instance;
                }
            }
        }

        public void Add(RequestResponse response)
        {
            lock (_lock)
            {
                _responses.Add((new Response(){
                    Cluster = Utils.ClusterInfo,
                    Heartbeat = CurrentState.Instance.Heartbeat.ToString(),
                    Jobs = RequestQueue.Instance.Count.ToString(),
                    Status = true,
                    Value = response.Value,
                    T0 = response.T0,
                    T1 = response.T1,
                    T2 = Utils.Millis,
                }.Serialize(), RequestQueue.Instance.GetReturnTopic(response.ID)));

                if (CurrentState.Instance.GetState == State.LEADER || CurrentState.Instance.GetState == State.SLEEPING)
                {
                    RequestQueue.Instance.DetachRequest(response.ID);
                } else 
                {
                    RequestQueue.Instance.CompleteRequest(response.ID);
                }
            }
        }

        private bool busy = false;
        public bool IsBusy 
        {
            get 
            {
                lock(_lock)
                {
                    return busy;
                }
            }
        }
        public void Empty()
        {
            lock(_lock)
            {
                if (busy) return;
                busy = true;
            }
            Task.Run(() => {
                lock(_lock)
                {
                    (string, string)[] arr = _responses.ToArray();
                    _responses.Clear();
                    Producer.Instance.SendMany(arr);
                    busy = false;
                }
            });
        }

    }
}