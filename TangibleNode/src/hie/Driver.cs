using System.Collections.Generic;
using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Linq;

namespace TangibleNode
{

    class PointResponse
    {
        public Dictionary<string, bool> Status {get; set;}
    }


    class DriverResponseHandler 
    {
        public void OnResponse(Driver driver, PointRequestBatch request, PointResponse response)
        {

            if (response!=null && response.Status != null)
            {
                request.Batch.ForEach((r0) => {
                    
                    if (response.Status.ContainsKey(r0.ID) && response.Status[r0.ID])
                    {
                        driver.RemoveRequestBehind(r0.ID);
                        driver.Heartbeat.Reset();
                    } else 
                    {
                        driver.AddRequestBehind(r0);
                    }
                });
            }
            driver.SetIsSending(false);
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
        public void StartClient(Driver driver, PointRequestBatch request) 
        {   
            // Connect to a remote device.  
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
                        byte[] msg = Encoder.EncodePointRequestBatch(request);

                        // Send the data through the socket.  
                        int bytesSent = sender.Send(msg);  

                        // Data buffer for incoming data.  
                        // TODO: implement proper parsing of incoming response data
                        byte[] bytes = new byte[6144];  // hope thats enough

                        // Receive the response from the remote device.  
                        int bytesRec = sender.Receive(bytes);  

                        if (bytesRec < 1) HandleFailure(driver, request);

                        PointResponse response = Encoder.DecodePointResponse(bytes);
                        if (response==null) HandleFailure(driver, request);
                        else new DriverResponseHandler().OnResponse(driver, request, response); 
                        
                        _notified = true;
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
        
        private void HandleFailure(Driver driver, PointRequestBatch request)
        {
            // Activate the responsehandler
            Dictionary<string, bool> r0 = new Dictionary<string, bool>();
            request.Batch.ForEach( (r) => {
                r0.Add(r.ID, false);
                }
            );
            new DriverResponseHandler().OnResponse(driver, request, new PointResponse(){
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
        private TDict<string, PointRequest> _requestsBehind {get;}
        public TInt Heartbeat {get;} = new TInt();
        private bool _sending {get; set;} = false;

        public Driver(DriverConfig Config, string Image)
        {
            this.Config = Config;
            this.Image = Image;
            _requestsBehind = new TDict<string, PointRequest>();
            _connector = new Connector(Config);
        }

        public void AddRequestBehind(PointRequest request)
        {
            if (!_requestsBehind.ContainsKey(request.ID))
                _requestsBehind.Add(request.ID, request);
        }

        public void RemoveRequestBehind(string requestID)
        {
            _requestsBehind.Remove(requestID);
        }

        public List<PointRequest> GetRequestsBehind()
        {
            return _requestsBehind.Values.ToList();
        }

        public void Write()
        {
            if (_requestsBehind.Count < 1 && IsSending) return;
            List<PointRequest> requests = GetRequestsBehind();
            if (requests.Count > 0)
            {
                SetIsSending(true);
                _connector.StartClient(this, 
                    new PointRequestBatch()
                    {
                        Batch = requests
                    });
            }
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
                Maintainer = Node.Self,
                Image = image
            };
            Docker.Instance.Containerize(config).GetAwaiter().GetResult();
            return new Driver(config, image);
        }
        public static Driver MakeDriver(Action action, int replica)
        {
            return MakeDriver(action.Image, replica);
        }
    }
}