using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TangibleNode 
{
    public class AsynchronousSocketListener
    {
        LingerOption _linger = new LingerOption(false, 0);

        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

    //     public void StartListening() 
    //     {  
    //         // Data buffer for incoming data.  
    //         byte[] bytes = new Byte[1024];  

    //         // Establish the local endpoint for the socket.  
    //         // Dns.GetHostName returns the name of the
    //         // host running the application.   
    //         IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(Params.HOST), Params.PORT);  

    //         // Create a TCP/IP socket.  
    //         Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  

    //         // Bind the socket to the local endpoint and
    //         // listen for incoming connections.  
    //         try {  
    //             listener.SendTimeout = Params.TIMEOUT;
    //             listener.ReceiveTimeout = Params.TIMEOUT;
    //             listener.Bind(localEndPoint);  
    //             listener.Listen();  

    //             // Start listening for connections.  
    //             while (true) {  

    //                 Socket handler = listener.Accept();  
                    
    //                 try 
    //                 {
    //                     // Program is suspended while waiting for an incoming connection.  

    //                     string data = null;

    //                     // An incoming connection needs to be processed.  
    //                     while (true) {  
    //                         try 
    //                         {
    //                             int bytesRec = handler.Receive(bytes);  
    //                             data += Encoding.ASCII.GetString(bytes,0,bytesRec);  
    //                             if (data.IndexOf("<EOF>") > -1) {  
    //                                 break;  
    //                             }  
    //                         } catch (Exception e)
    //                         {
    //                             Console.WriteLine(e.ToString());
    //                         }
    //                     }  

    //                     Response response1 = new Response(){Completed = null, Data = null, Status = null};;
    //                     try 
    //                     {
    //                         RequestBatch requestBatch = Encoder.DecodeRequestBatch(data);
    //                         StateLog.Instance.Peers.AddIfNew(requestBatch.Sender);
    //                         response1 = MakeResponse(requestBatch);
    //                     } catch (Exception e)
    //                     {   
    //                         Console.WriteLine(e.ToString());
    //                     }
                            
    //                     int bytesSend = handler.Send(Encoder.EncodeResponse(response1));  

    //                 } catch (Exception e)
    //                 {
    //                     Console.WriteLine(e.ToString());
    //                 } finally {
    //                     handler.Shutdown(SocketShutdown.Both);  
    //                     handler.Close(0);  
    //                 }
    //             }  

    //         } catch (Exception e) {  
    //             Console.WriteLine("FATAL:" + e.ToString());  
    //         }  
    // }  

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
                listener.SendTimeout = Params.TIMEOUT;
                listener.ReceiveTimeout = Params.TIMEOUT;
                listener.Bind(localEndPoint);  
                listener.Listen();  
                Logger.Write(Logger.Tag.INFO,"Started " + Params.ID + " on "+Params.HOST+":"+Params.PORT);
                while (true) 
                {  
                    try 
                    {
                        // Set the event to nonsignaled state.  
                        allDone.Reset();  

                        // Start an asynchronous socket to listen for connections.  
                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),  
                            listener );  

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();  
                    } catch (Exception e)
                    {   
                        Console.WriteLine(e.ToString());
                    }
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

                if (bytesRead > 0 && state.Retries<2) {  
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(  
                        state.buffer, 0, bytesRead));  

                    // Check for end-of-file tag. If it is not there, read
                    // more data.  
                    content = state.sb.ToString();  
                    if (content.IndexOf("<EOF>") > -1 || state.Retries>2) {  
                        // All the data has been read from the
                        // client.         

                        Response response1 = new Response(){Completed = null, Data = null, Status = null};;
                        try 
                        {
                            RequestBatch requestBatch = Encoder.DecodeRequestBatch(content);
                            StateLog.Instance.Peers.AddIfNew(requestBatch.Sender);
                            response1 = MakeResponse(requestBatch);
                        } catch (Exception e)
                        {   
                            Console.WriteLine(e.ToString());
                        }
                            
                        Send(handler, Encoder.EncodeResponse(response1));  
                        
                    } else {  
                        // Not all data received. Get more.  
                        state.Retries+=1;
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                        new AsyncCallback(ReadCallback), state);  
                    }  
                }  
            } catch (Exception e) 
            {
                Logger.Write(Logger.Tag.ERROR, e.ToString());
            }
        }

        private Response MakeResponse(RequestBatch requestBatch)
        {
            Dictionary<string, bool> response = new Dictionary<string, bool>();

            if (requestBatch.Sender == null)
            {
                foreach (Request request in requestBatch.Batch)
                {
                    if (request.Type == Request._Type.POINT)
                    {
                        ValueResponse vr = Encoder.DecodeValueResponse(request.Data);
                        if (CurrentState.Instance.IsLeader)
                        {
                            StateLog.Instance.Leader_AddActionCompleted(vr.ActionID);
                        }
                        else 
                        {
                            StateLog.Instance.Follower_MarkActionCompleted(vr.ActionID);
                        }
                        StateLog.Instance.RemoveCurrentTask(vr.ActionID);
                        if (Params.TEST_RECEIVER_HOST!=string.Empty)
                        {
                            try 
                            {
                                TestReceiverClient.Instance.AddEntry(vr);
                            } catch (Exception e)
                            {
                                Logger.Write(Logger.Tag.ERROR, "ADD_ENTRY: " + e.ToString());
                            }
                        }
                        response.Add(request.ID, true);
                    } else 
                    {
                        response.Add(request.ID, false);
                    }
                }

                return new Response(){
                    Status = response
                };
            }
            else 
            {
                foreach (Request request in requestBatch.Batch)
                {
                    // if (request.Type == Request._Type.POINT)
                    // {
                    //     ValueResponse vr = Encoder.DecodeValueResponse(request.Data);
                    //     if (vr.Complete)
                    //     {
                    //         if (CurrentState.Instance.IsLeader)
                    //         {
                    //             StateLog.Instance.Leader_AddActionCompleted(vr.ActionID);
                    //         }
                    //         else 
                    //         {
                    //             StateLog.Instance.Follower_MarkActionCompleted(vr.ActionID);
                    //         }
                    //     }
                    //     StateLog.Instance.RemoveCurrentTask(vr.ActionID);
                    //     if (Params.TEST_RECEIVER_HOST!=string.Empty)
                    //     {
                    //         TestReceiverClient.Instance.AddEntry(vr);
                    //     }
                    // } 
                    // else 
                    if (request.Type == Request._Type.VOTE)
                    {
                        Vote vote = Encoder.DecodeVote(request.Data);
                        Vote myVote = vote;
                        int count = StateLog.Instance.LogCount;
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
                        response.Add(request.ID, true);
                        return new Response()
                        {
                            Status = response,
                            Data = Encoder.EncodeVote(myVote)
                        };

                    } else if (request.Type == Request._Type.ACTION)
                    {
                        Action action = Encoder.DecodeAction(request.Data);
                        action.T1 = Utils.Micros.ToString();
                        StateLog.Instance.AppendAction(action);
                        CurrentState.Instance.CancelState();
                        response.Add(request.ID, true);
                    } else if (request.Type == Request._Type.NODE_ADD)
                    {
                        Node node = Encoder.DecodeNode(request.Data);
                        StateLog.Instance.Peers.AddNewNode(node);
                        CurrentState.Instance.CancelState();
                        response.Add(request.ID, true);
                    }
                    else if (request.Type == Request._Type.NODE_DEL)
                    {
                        Node node = Encoder.DecodeNode(request.Data);
                        StateLog.Instance.ClearPeerLog(node.ID);
                        StateLog.Instance.Peers.TryRemoveNode(node.ID);
                        CurrentState.Instance.CancelState();
                        response.Add(request.ID, true);
                    } else 
                    {
                        response.Add(request.ID, false);
                    }
                }

                if (requestBatch.Completed!=null) foreach (string actionID in requestBatch.Completed)
                {
                    StateLog.Instance.Follower_AddActionCompleted(actionID);
                }
                
                if (requestBatch.Sender!=null && requestBatch.Step>Params.STEP) Params.STEP = requestBatch.Step;
                
                List<string> completed = null;

                if (requestBatch.Sender!=null) 
                {
                    completed = StateLog.Instance.Follower_GetCompletedActions();
                    CurrentState.Instance.Timer.Reset();
                } 

                return new Response()
                {
                    Status = response,
                    Completed = completed
                };
            }
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