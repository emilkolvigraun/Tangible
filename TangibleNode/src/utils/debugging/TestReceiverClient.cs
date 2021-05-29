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

        private TDict<string, string> _responsesNotSend = new TDict<string, string>();
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

        public void AddEntry(HashSet<ValueResponse> response)
        {
            lock(_lock)
            {

                foreach(ValueResponse r in response)
                {
                    _responsesNotSend.Add(Utils.GenerateUUID(), Encoder.SerializeObjectIndented(r));

                }
            }
        }

        public void AddEntryError(string response)
        {
            lock(_lock)
            {

                _responsesNotSend.Add(Utils.GenerateUUID(), Encoder.SerializeObjectIndented(new ErrorResponse {
                    Status = false,
                    Message = response
                }));
            }
        }

        public RequestResponse GetBatch
        {
            get 
            {
                lock(_lock)
                {
                    if (Count < 1) return null;
                    RequestResponse rs = new RequestResponse(){Batch=new Dictionary<string, string>()};
                    foreach (KeyValuePair<string, string> r in _responsesNotSend.ToList())
                    {
                        rs.Batch.Add(r.Key, r.Value);
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
                            byte[] bytes = new byte[2048*Params.BATCH_SIZE];  // hope thats enough
                            // byte[] bytes = new byte[10240];  // hope thats enough
                            // Receive the response from the remote device.  
                            int bytesRec = sender.Receive(bytes);  

                            if (bytesRec < 1) HandleFailure(response);

                            try 
                            {
                                StatusResponse r1 = Encoder.DecodePointResponse(bytes);
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
            foreach (KeyValuePair<string, string> r in response.Batch.ToList())
            {
                r2.Add(r.Key, false);
            }
            OnResponse(response, new StatusResponse(){
                Status = r2
            });
            _notified = true;
        }

        private void OnResponse(RequestResponse sender, StatusResponse receiver)
        {
            foreach (KeyValuePair<string, string> s in sender.Batch.ToList())
            {
                if (receiver!=null&&receiver.Status!=null&&receiver.Status.ContainsKey(s.Key)&&receiver.Status[s.Key])
                {
                    lock(_lock)
                    {
                        _responsesNotSend.Remove(s.Key);
                    }
                }
            }
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