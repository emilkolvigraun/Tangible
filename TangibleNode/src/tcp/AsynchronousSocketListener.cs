using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TangibleNode 
{
    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// Establishes the local endpoint for the socket
        /// and starts listening for incoming connections
        /// </summary>
        public void StartListening()
        {
            // Establish the local endpoint for the socket.   
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(Params.HOST), Params.PORT);  

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
        }

        public void ReadCallback(IAsyncResult ar)
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
                    StateLog.Instance.Peers.AddIfNew(requestBatch.Sender);
                    Response response = MakeResponse(requestBatch);
                    
                    // send back the response to client
                    Send(handler, Encoder.EncodeResponse(response));  
                } else {  
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReadCallback), state);  
                }  
            }  
        }

        private Response MakeResponse(RequestBatch requestBatch)
        {
            Dictionary<string, bool> response = new Dictionary<string, bool>();
            foreach (Request request in requestBatch.Batch)
            {
                response.Add(request.ID, true);

                if (request.Type == Request._Type.VOTE){
                    Vote vote = Encoder.DecodeVote(request.Data);
                    Vote myVote = vote;
                    int count = StateLog.Instance.Peers.LogCount;
                    if (vote.LogCount < count)
                    {
                        myVote = new Vote()
                        {
                            ID = Params.ID,
                            LogCount = count
                        };
                    }
                    else if (Utils.IsCandidate(CurrentState.Instance.Get_State.State)) 
                    {
                        CurrentState.Instance.CancelState();
                        CurrentState.Instance.Timer.Reset(((int)(Params.HEARTBEAT_MS/2)));
                    }
                    else CurrentState.Instance.Timer.Reset();
                    return new Response()
                    {
                        Status = response,
                        Data = Encoder.EncodeVote(myVote)
                    };

                } else if (request.Type == Request._Type.ACTION)
                {
                    Action action = Encoder.DecodeAction(request.Data);
                    StateLog.Instance.AppendAction(action);
                } else if (request.Type == Request._Type.NODE_ADD)
                {
                    Node node = Encoder.DecodeNode(request.Data);
                    StateLog.Instance.Peers.AddNewNode(node);
                }
                else if (request.Type == Request._Type.NODE_DEL)
                {
                    Node node = Encoder.DecodeNode(request.Data);
                    StateLog.Instance.ClearPeerLog(node.ID);
                    StateLog.Instance.Peers.TryRemoveNode(node.ID);
                }
            }

            foreach (string actionID in requestBatch.Completed)
                StateLog.Instance.Follower_AddActionCompleted(actionID);

            CurrentState.Instance.Timer.Reset();

            return new Response()
            {
                Status = response,
                Completed = StateLog.Instance.Follower_GetCompletedActions()
            };
        }

        private void Send(Socket handler, byte[] byteData)
        {
            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,  
                new AsyncCallback(SendCallback), handler);  
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket) ar.AsyncState;  

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);  

                handler.Shutdown(SocketShutdown.Both);  
                handler.Close();  

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());  
            }  
        }
    }
}