using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TangibleDriver 
{
    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);
        private Handler _handler {get;}

        public AsynchronousSocketListener(Handler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Establishes the local endpoint for the socket
        /// and starts listening for incoming connections
        /// </summary>
        public void StartListening()
        {
            // Establish the local endpoint for the socket.   
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Params.PORT);  

            // Create a TCP/IP socket.  
            Socket listener = new Socket(AddressFamily.InterNetwork,  
                SocketType.Stream, ProtocolType.Tcp );  

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try {  
                listener.Bind(localEndPoint);  
                listener.Listen(100);  
                Logger.Write(Logger.Tag.INFO,"Started " + Params.ID + " on "+Params.HOST+":"+Params.PORT);
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
            } catch {allDone.Set();}
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
                        RequestBatch requestBatch = Encoder.DecodeRequestBatch(content);
                        (Response response, Request point) response = MakeResponse(requestBatch);
                        
                        // send back the response to client
                        Send(handler, Encoder.EncodeResponse(response.response));  
                        Task.Run(() => {_handler.ProcessRequest(response.point);});
                    } else {  
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                        new AsyncCallback(ReadCallback), state);  
                    }  
                }  
            } catch (Exception e){Logger.Write(Logger.Tag.ERROR, e.ToString());}
        }

        private (Response response, Request point) MakeResponse(RequestBatch requestBatch)
        {
            Dictionary<string, bool> response = new Dictionary<string, bool>();
            foreach (Request request in requestBatch.Batch)
            {
                try 
                {
                    response.Add(request.ID, true);
                    return (new Response()
                    {
                        Status = response
                    }, request);
                } catch
                {
                    response.Add(request.ID, false);
                }
            }
            return (new Response()
            {
                Status = response
            }, null);
        }

        private void Send(Socket handler, byte[] byteData)
        {
            try 
            {
                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0,  
                    new AsyncCallback(SendCallback), handler);  
            } catch {}
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                // Retrieve the socket from the state object.  
                handler = (Socket) ar.AsyncState;  

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);  
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());  
            }  

            try
            {
                if (handler != null)
                {
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close();  
                }
            }
            catch {}  
        }
    }
}