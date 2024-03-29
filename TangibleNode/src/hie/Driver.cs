using System.Collections.Generic;
using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace TangibleNode
{

    class StatusResponse
    {
        public Dictionary<string, bool> Status {get; set;}
    }


    class DriverResponseHandler 
    {
        public void OnResponse(Driver driver, DataRequestBatch request, StatusResponse response)
        {
            if (driver.Heartbeat.Value > Params.MAX_RETRIES)
            {
                CurrentState.Instance.SetHIEVar(false);
                request.Batch.ForEach((r0) => {
                    driver.CurrentlySending.Remove(r0.ID);
                    driver.RemoveRequestBehind(r0.ID);
                    if (CurrentState.Instance.IsLeader)
                    {
                        StateLog.Instance.Leader_AddActionCompleted(r0.ID, Params.ID);
                    }
                    else 
                    {
                        StateLog.Instance.Follower_MarkActionCompleted(r0.ID);
                    }
                    StateLog.Instance.RemoveCurrentTask(r0.ID);

                    Logger.Write(Logger.Tag.WARN, "Failed to execute data-request [entry:" + r0.ID.Substring(0,10) + "]");

                    if (Params.TEST_RECEIVER_HOST!=string.Empty)
                    {
                        try 
                        {
                            TestReceiverClient.Instance.AddEntryError("Failed to deploy driver.");
                        } catch (Exception e)
                        {
                            Logger.Write(Logger.Tag.ERROR, "ADD_ENTRY: " + e.ToString());
                        }
                    }
                });

            } else if (response!=null && response.Status != null)
            {
                request.Batch.ForEach((r0) => {
                    driver.CurrentlySending.Remove(r0.ID);
                    
                    if (response.Status.ContainsKey(r0.ID) && response.Status[r0.ID])
                    {
                        driver.RemoveRequestBehind(r0.ID);
                        driver.Heartbeat.Reset();
                        CurrentState.Instance.SetHIEVar(true);
                    } else 
                    {
                        driver.AddRequestBehind(r0);
                    }
                });
            } else 
            {
                request.Batch.ForEach((r0) => {
                    driver.CurrentlySending.Remove(r0.ID);
                    driver.AddRequestBehind(r0);
                });
            }
            // driver.SetIsSending(false);
        }
    }


    class Connector
    {
        LingerOption _linger = new LingerOption(false, 0);
        StringBuilder _received = new StringBuilder();

        public string Host {get;}
        public int Port {get;}
        public string ID {get;}
        private bool _notified {get; set;} = false;

        public Connector(DriverConfig config)
        {
            Host = config.Host;
            Port = config.Port;
            ID = config.ID;
        }
    
        // The response from the remote device.  
        IPEndPoint remoteEP = null;
                // Create a TCP/IP  socket.  
        Socket sender = null;
    
        /// <summary>
        /// Establishes connection to the remote host, sends the request an activates OnResponse on the requesthandler
        /// </summary>
        public void StartClient(Driver driver, DataRequestBatch request) 
        {   
            _received.Clear();

            remoteEP = new IPEndPoint(IPAddress.Parse(Host),Port); 
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );  
            
            try {  
                _notified = false;
                sender.LingerState = _linger;
                // Establish the remote endpoint for the socket.   
                try {  

                    IAsyncResult result = sender.BeginConnect(remoteEP, null, null);

                    bool success = result.AsyncWaitHandle.WaitOne(Params.TIMEOUT, true);

                    if (success && sender.Connected)
                    {
                        sender.EndConnect(result);
                        
                        // Encode the data string into a byte array.  
                        byte[] msg = Encoder.EncodeDataRequestBatch(request);

                        // Send the data through the socket.  
                        bool sendSuccess = Task.Run(() => {

                            int bytesSent = sender.Send(msg);  
                            // Data buffer for incoming data.  
                            // TODO: implement proper parsing of incoming response data
                            byte[] bytes = new byte[2048*Params.BATCH_SIZE];  // hope thats enough
                            // byte[] bytes = new byte[10240];  // hope thats enough
                            // Receive the response from the remote device.  
                            int bytesRec = sender.Receive(bytes);  

                            if (bytesRec < 1) HandleFailure(driver, request);

                            StatusResponse response = Encoder.DecodePointResponse(bytes);
                            if (response==null) HandleFailure(driver, request);
                            else new DriverResponseHandler().OnResponse(driver, request, response); 
                            
                            _notified = true;
                        }).Wait(Params.TIMEOUT);

                        if (!sendSuccess && !_notified) HandleFailure(driver, request);
                        if (!_notified) HandleFailure(driver, request);
                    }
                } catch (ArgumentNullException ane) {  
                    Logger.Write(Logger.Tag.ERROR, string.Format("ArgumentNullException : {0}", ane.ToString()));
                } catch (SocketException se) {  
                    Logger.Write(Logger.Tag.ERROR, string.Format("ArgumentNullException : {0}", se.ToString()));
                } catch (Exception e) {  
                    Logger.Write(Logger.Tag.ERROR, string.Format("ArgumentNullException : {0}", e.ToString()));
                }  
            } catch (Exception e) {  
                    Logger.Write(Logger.Tag.ERROR, e.ToString());
            } finally {
                try 
                {
                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both); 
                    sender.Close(0);
                } catch {}
            }

            if (!_notified) 
                HandleFailure(driver, request);
        }  
        
        private void HandleFailure(Driver driver, DataRequestBatch request)
        {
            // Activate the responsehandler
            Dictionary<string, bool> r0 = new Dictionary<string, bool>();
            request.Batch.ForEach( (r) => {
                r0.Add(r.ID, false);
                }
            );
            new DriverResponseHandler().OnResponse(driver, request, new StatusResponse(){
                Status = r0
            });  
            
            Logger.Write(Logger.Tag.WARN, "Unable to connect to [driver:" + driver.Config.ID.Substring(0,10) +"..., retries:"+driver.Heartbeat.Value.ToString()+"]");
            driver.Heartbeat.Increment();
            _notified = true;
        }
    }

    class Driver 
    {
        public DriverConfig Config {get;}
        public string Image {get;}
        private Connector _connector {get;}
        private TDict<string, DataRequest> _requestsBehind {get;}
        public TInt Heartbeat {get;} = new TInt();
        private bool _sending {get; set;} = false;
        public TSet<string> CurrentlySending {get;} = new TSet<string>();

        public int BehindCount 
        {
            get 
            {
                return _requestsBehind.Count;
            }
        }

        public Driver(DriverConfig Config, string Image)
        {
            this.Config = Config;
            this.Image = Image;
            _requestsBehind = new TDict<string, DataRequest>();
            _connector = new Connector(Config);
        }

        public void AddRequestBehind(DataRequest request)
        {
            if (!_requestsBehind.ContainsKey(request.ID))
                _requestsBehind.Add(request.ID, request);
        }

        public void RemoveRequestBehind(string requestID)
        {
            _requestsBehind.Remove(requestID);
        }

        public List<DataRequest> GetRequestsBehind()
        {
            if (_requestsBehind.Count < 1) return new List<DataRequest>();
            List<DataRequest> requests = new List<DataRequest>();
            foreach (DataRequest r0 in _requestsBehind.Values.ToList())
            {
                if (!CurrentlySending.Contains(r0.ID))
                {
                    requests.Add(r0);
                    // FileLogger.Instance.AppendEntry(r0.Value, r0.PointDetails.Count.ToString(), Utils.Micros.ToString(), "timestamp", Params.ID, "driver");
                    CurrentlySending.Add(r0.ID);
                    // int s = (Params.BATCH_SIZE-10)/r0.PointIDs.Count;
                    // // if (s>8)s=8;
                    // if (requests.Count >= s) break;
                }
            }
            return requests;
        }

        public void Write()
        {
            
            if (IsSending) return;

            // Task.Run(() => {
                SetIsSending(true);
                // if (_requestsBehind.Count < 1 && IsSending) return;
                List<DataRequest> requests = GetRequestsBehind();

                if (requests.Count > 0)
                {
                // SetIsSending(true);
                _connector.StartClient(this, 
                    new DataRequestBatch()
                    {
                        Batch = requests
                    });
                    
                }// {
                    // requests = GetRequestsBehind();
                // }

                SetIsSending(false);
            // });
        }

        private readonly object _lock = new object();
        public bool IsSending 
        {
            get 
            {
                lock (_lock)
                {
                    return _sending;
                }
            }
        }

        public void SetIsSending(bool b)
        {
            lock (_lock)
            {
                _sending = b;
            }
        }

        public static Driver MakeDriver(string image, int replica)
        {
            string name = Params.ID+"_"+image.Replace("-","_").Replace("/","_").Replace(" ", "")+"_"+replica.ToString();
            DriverConfig config = new DriverConfig(){
                ID = name,
                Host = Params.DOCKER_HOST,
                Port = Params.GetUnusedPort(),
                AssociatedNode = Credentials.Self,
                Image = image,
                Replica = replica
            };

            try 
            {
                (bool running, string id) info = Docker.Instance.IsContainerRunning(name).GetAwaiter().GetResult();
                
                if (info.running==true && info.id != null)
                {
                    Docker.Instance.StopContainers(info.id).GetAwaiter().GetResult();
                    Docker.Instance.RemoveStoppedContainers().GetAwaiter().GetResult();
                }
            } catch { }

            while (true)
            {
                try 
                {
                    Docker.Instance.Containerize(config).GetAwaiter().GetResult();
                    break;
                }
                catch 
                {
                    Utils.Sleep(Utils.GetRandomInt(Params.ELECTION_TIMEOUT_START, Params.ELECTION_TIMEOUT_END));
                    (bool running, string id) info = Docker.Instance.IsContainerRunning(name).GetAwaiter().GetResult();
                    // Console.WriteLine("pulled" + info.id + " " + info.running.ToString());
                    if (info.running) break;
                }
            }
            return new Driver(config, image);
        }
        // public static Driver MakeDriver(DataRequest action, int replica)
        // {
        //     return MakeDriver(action.Image, replica);
        // }
    }
}