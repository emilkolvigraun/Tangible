using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Collections.Generic; 
using System.Linq;
using System.Threading.Tasks;

namespace TangibleNode
{


    class TestReceiverClient 
    {
        LingerOption _linger = new LingerOption(false, 0);
        StringBuilder _received = new StringBuilder();
        private bool _notified {get; set;} = false;

        // The response from the remote device.  
        IPEndPoint remoteEP = null;
                // Create a TCP/IP  socket.  
        Socket sender = null;

        private TDict<string, ESBResponse> _responsesNotSend = new TDict<string, ESBResponse>();
        private readonly object _lock = new object();

        public int Count 
        {
            get 
            {
                lock(_lock)
                {
                    return _responsesNotSend.Count;
                }
            }
        }

        public void AddEntry(ValueResponse response)
        {
            lock(_lock)
            {
                ESBResponse esbResponse = new ESBResponse()
                {
                    ID = Utils.GenerateUUID(),
                    Message = response.Message,
                    Timestamp = Utils.Micros.ToString(),
                    T01234 = response.T0123+","+response.Timestamp
                };
                _responsesNotSend.Add(esbResponse.ID, esbResponse);
            }
        }

        public RequestResponse GetBatch
        {
            get 
            {
                lock(_lock)
                {
                    if (Count < 1) return null;
                    RequestResponse rs = new RequestResponse(){Batch=new List<ESBResponse>()};
                    foreach (ESBResponse r in _responsesNotSend.Values.ToList())
                    {
                        rs.Batch.Add(r);
                        if (rs.Batch.Count >= Params.BATCH_SIZE) break;
                    }
                    return rs;
                }
            }
        }

        public void StartClient()
        {   
            RequestResponse response = GetBatch;
            if (response == null) return;

            // Connect to a remote device.  
            _received.Clear();
            remoteEP = new IPEndPoint(IPAddress.Parse(Params.TEST_RECEIVER_HOST), Params.TEST_RECEIVER_PORT); 
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );  
            
            try {  
                _notified = false;
                sender.LingerState = _linger;
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                
                // Connect the socket to the remote endpoint. Catch any errors.  
                try {  

                    IAsyncResult result = sender.BeginConnect(remoteEP, null, null);

                    bool success = result.AsyncWaitHandle.WaitOne(Params.TIMEOUT, true);

                    if (success && sender.Connected)
                    {
                        sender.EndConnect(result);
                        
                        // Encode the data string into a byte array.  
                        byte[] msg = Encoder.EncodeRequestResponse(response);

                        // Send the data through the socket.  
                        int bytesSent = sender.Send(msg);  

                        // bool sendSuccess = Task.Run(() => {
                            // Data buffer for incoming data.  
                            byte[] bytes = new byte[1024*Params.BATCH_SIZE];  // hope thats enough

                            // Receive the response from the remote device.  
                            int bytesRec = sender.Receive(bytes);  

                            if (bytesRec < 1) HandleFailure(response);

                            try 
                            {
                                PointResponse r1 = Encoder.DecodePointResponse(bytes);
                                OnResponse(response, r1); 
                                _notified = true;
                            }
                            catch {HandleFailure(response);}
                        // }).Wait(Params.TIMEOUT);

                        if (!_notified) HandleFailure(response);
                        // if (!sendSuccess && !_notified) HandleFailure(response);
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
                HandleFailure(response);
        }  

        private void HandleFailure(RequestResponse response)
        {
            Dictionary<string, bool> r2 = new Dictionary<string, bool>();
            response.Batch.ForEach((r) => {
                r2.Add(r.ID, false);
            });
            OnResponse(response, new PointResponse(){
                Status = r2
            });
            _notified = true;
        }

        private void OnResponse(RequestResponse sender, PointResponse receiver)
        {
            sender.Batch.ForEach((s) => {
                if (receiver!=null&&receiver.Status!=null&&receiver.Status.ContainsKey(s.ID)&&receiver.Status[s.ID])
                {
                    lock(_lock)
                    {
                        _responsesNotSend.Remove(s.ID);
                    }
                }
            });
        }

        private static TestReceiverClient _instance = null;
        private static readonly object _lock_1 = new object();

        public static TestReceiverClient Instance 
        {
            get 
            {
                lock(_lock_1)
                {
                    if (_instance==null)_instance=new TestReceiverClient();
                    return _instance;
                }
            }
        }
    }
}