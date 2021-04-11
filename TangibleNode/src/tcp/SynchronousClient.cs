using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Collections.Generic;  
using System.Threading.Tasks;

namespace TangibleNode 
{
    public class SynchronousClient 
    {   
        LingerOption _linger = new LingerOption(false, 0);
        StringBuilder _received = new StringBuilder();

        public string Host {get;}
        public int Port {get;}
        public string ID {get;}

        private bool _notified {get; set;} = false;

        public SynchronousClient(string host, int port, string id)
        {
            Host = host;
            Port = port;
            ID = id;
        }
    
        // The response from the remote device.  
        IPEndPoint remoteEP = null;
                // Create a TCP/IP  socket.  
        Socket sender = null;
    
        /// <summary>
        /// Establishes connection to the remote host, sends the request an activates OnResponse on the requesthandler
        /// </summary>
        public void StartClient(RequestBatch request, IResponseHandler rh) 
        {   
            // Connect to a remote device.  
            _received.Clear();
            remoteEP = new IPEndPoint(IPAddress.Parse(Host),Port); 
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
                        byte[] msg = Encoder.EncodeRequestBatch(request);

                        // Send the data through the socket.  
                        
                        int bytesSent = sender.Send(msg);  

                        // bool sendSuccess = Task.Run(()=>{
                        // Data buffer for incoming data.  
                        byte[] bytes = new byte[2048*Params.BATCH_SIZE];  // hope thats enough
                        // byte[] bytes = new byte[10240];  // hope thats enough

                        // Receive the response from the remote device.  
                        int bytesRec = sender.Receive(bytes);  

                        if (bytesRec < 1) HandleFailure(request, rh);

                        rh.OnResponse(ID, request, Encoding.ASCII.GetString(bytes)); 
                        _notified = true;
                        // }).Wait(Params.TIMEOUT);

                        // if (!sendSuccess && !_notified) HandleFailure(request, rh);
                    }
                } catch (ArgumentNullException ane) {  
                    Logger.Write(Logger.Tag.ERROR, string.Format("ArgumentNullException : {0}", ane.ToString()));
                } catch (SocketException) {  
                    // Logger.Write(Logger.Tag.ERROR, string.Format("ArgumentNullException : {0}", se.ToString()));
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
                HandleFailure(request, rh);
        }  
        
        private void HandleFailure(RequestBatch request, IResponseHandler rh)
        {
            Dictionary<string, bool> r0 = new Dictionary<string, bool>();
            foreach (Request r1 in request.Batch)
            {
                r0.Add(r1.ID, false);
            }

            // Activate the responsehandler
            rh.OnResponse(ID, request, Encoder.SerializeResponse(new Response(){
                Status = r0,
                Completed = null,
                Data = null
            }));  
            
            Logger.Write(Logger.Tag.WARN, "Unable to connect to [node:" + ID +"][retries:"+StateLog.Instance.Peers.GetHeartbeat(ID)+"]");
            StateLog.Instance.Peers.AccessHeartbeat(ID, (hb) => {hb.Increment();});   
            _notified = true;
        }
    }  
}