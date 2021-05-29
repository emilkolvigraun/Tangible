using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestReceiver 
{
    public class AsynchronousSocketListener
    {
        LingerOption _linger = new LingerOption(false, 0);

        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// Establishes the local endpoint for the socket
        /// and starts listening for incoming connections
        /// </summary>
        public void StartListening()
        {
            // Establish the local endpoint for the socket.   
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(Program.HOST), Program.PORT);  

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork,  
                SocketType.Stream, ProtocolType.Tcp );  

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try {  
                listener.SendTimeout = 8000;
                listener.ReceiveTimeout = 8000;
                listener.Bind(localEndPoint);  
                listener.Listen(100);  
                while (true) {  
                    // Set the event to nonsignaled state.  
                    allDone.Reset();  

                    // Start an asynchronous socket to listen for connections.  
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),  
                        listener );  

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();  
                }  

            } catch (Exception e) {  
                Console.WriteLine(e.ToString());
            }  
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try 
            {
                // Signal the main thread to continue.  
                allDone.Set();  

                // Get the socket that handles the client request.  
                Socket listener = (Socket) ar.AsyncState;  
                Socket handler = listener.EndAccept(ar);  

                // Create the state object.  
                StateObject state = new StateObject();  
                state.workSocket = handler;  
                handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReadCallback), state);  
            } catch (Exception e) {  
                Console.WriteLine(e.ToString());  
            }  
        }

        public void ReadCallback(IAsyncResult ar)
        {
            try 
            {
                String content = String.Empty;  

                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject state = (StateObject) ar.AsyncState;  
                Socket handler = state.workSocket;  

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);  

                if (bytesRead > 0) {  
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(  
                        state.buffer, 0, bytesRead));  

                    // Check for end-of-file tag. If it is not there, read
                    // more data.  
                    content = state.sb.ToString();  
                    if (content.IndexOf("<EOF>") > -1) {  
                        // All the data has been read from the
                        // client.         

                        StatusResponse response1 = null;
                        try 
                        {
                            RequestResponse requestBatch = Encoder.DecodeRequestResponse(content);
                            response1 = MakeResponse(requestBatch);
                        } catch (Exception e) 
                        {
                            Console.WriteLine(e.ToString());
                            response1 = new StatusResponse(){Status = null};
                        }
                            
                        Send(handler, Encoder.EncodePointResponse(response1));  
                        
                    } else {  
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                        new AsyncCallback(ReadCallback), state);  
                    }  
                }  
            } catch (Exception e) {  
                Console.WriteLine(e.ToString());
            }  
        }

        private StatusResponse MakeResponse(RequestResponse requestBatch)
        {
            try 
            {
                Dictionary<string, bool> response = new Dictionary<string, bool>();
                foreach (KeyValuePair<string, string> request in requestBatch.Batch)
                {
                    response.Add(request.Key, true);
                    Logger.Write(request.Value);
                } 
                return new StatusResponse(){
                    Status = response
                };
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return null;
        }

        private void Send(Socket handler, byte[] byteData)
        {
            try 
            {
                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0,  
                    new AsyncCallback(SendCallback), handler);  
            } catch (Exception e) {  
                Console.WriteLine(e.ToString());  
            }  
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket) ar.AsyncState;  

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);  
                handler.LingerState = _linger;
                handler.Shutdown(SocketShutdown.Both);  
                handler.Close(0);  
                
            } catch (Exception e) {  
                Console.WriteLine(e.ToString());  
            }  
        }
    }
}