using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Threading;  
using System.Text;
using System.Collections.Generic;  

namespace TangibleDriver 
{
    public class AsynchronousClient {          
        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);  
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);  
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);  

        private bool _notified {get; set;} = false;

        // The response from the remote device.  
        private String response = String.Empty;  
    
        /// <summary>
        /// Establishes connection to the remote host, sends the request an activates OnResponse on the requesthandler
        /// </summary>
        public void StartClient(RequestBatch request, IResponseHandler rh) {  
            // Connect to a remote device.  
            try {  
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(Params.NODE_HOST), Params.NODE_PORT);  
    
                // Create a TCP/IP socket.  
                Socket client = new Socket(AddressFamily.InterNetwork,  
                    SocketType.Stream, ProtocolType.Tcp);  

                // Connect to the remote endpoint.  
                IAsyncResult result = client.BeginConnect( remoteEP,
                    new AsyncCallback(ConnectCallback), client);  

                connectDone.WaitOne(((int)(Params.TIMEOUT)), true);  
                // connectDone.WaitOne(); 

                
                if (client.Connected)
                {    
                    // Send test data to the remote device.  
                    Send(client, Encoder.EncodeRequestBatch(request));  
                    sendDone.WaitOne();  
        
                    // Receive the response from the remote device.  
                    Receive(client);  
                    receiveDone.WaitOne();  
        
                    // Activate the responsehandler
                    rh.OnResponse(Params.NODE_NAME, request, response);  
                    _notified = true;
                } 

                try 
                {
                    // Release the socket.  
                    client.Shutdown(SocketShutdown.Both);  
                } catch {}
                try 
                {
                    // Release the socket.  
                    client.Close();  
                } catch {}
    
                connectDone.Reset();  
                sendDone.Reset();  
                receiveDone.Reset();  
                response = String.Empty;   
            } catch (Exception e)
            {  
                Logger.Write(Logger.Tag.ERROR, e.ToString());
                connectDone.Reset();  
                sendDone.Reset();  
                receiveDone.Reset();  
                response = String.Empty;   
                HandleFailure(request, rh);
            } 

            if (!_notified)
            {
                HandleFailure(request, rh);
            } else 
            {
                _notified = false;
            }
        }  
        
        private void HandleFailure(RequestBatch request, IResponseHandler rh)
        {
            Dictionary<string, bool> r0 = new Dictionary<string, bool>();
            foreach (Request r1 in request.Batch)
            {
                r0.Add(r1.ID, false);
            }

            // Activate the responsehandler
            rh.OnResponse(Params.NODE_NAME, request, Encoder.SerializeResponse(new Response(){
                Status = r0
            }));  
            
            _notified = true;
        }
    
        private void ConnectCallback(IAsyncResult ar) {  
            try {  
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;  

                if (client.Connected)
                {
                    // Complete the connection.  
                    client.EndConnect(ar);  
                    // Signal that the connection has been made.  
                    connectDone.Set();                  
                }
    
            } catch
            {  
            }  
        }  
    
        private void Receive(Socket client) {  
            try {  
                // Create the state object.  
                StateObject state = new StateObject();  
                state.workSocket = client;  
    
                // Begin receiving the data from the remote device.  
                client.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,  
                    new AsyncCallback(ReceiveCallback), state);  
            } catch 
            {  
            }  
        }  
    
        private void ReceiveCallback( IAsyncResult ar ) {  
            try {  
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject) ar.AsyncState;  
                Socket client = state.workSocket;  
    
                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar); 
    
                if (bytesRead > 0) {  
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer,0,bytesRead));  
    
                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer,0,StateObject.BufferSize,0,  
                        new AsyncCallback(ReceiveCallback), state);  
                } else {  
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1) {  
                        response = state.sb.ToString();  
                    }  
                    // Signal that all bytes have been received.  
                    receiveDone.Set();  
                }  
            } catch
            {
            }  
        }  
    
        private void Send(Socket client, byte[] byteData) {     
            try 
            {
                // Begin sending the data to the remote device.  
                client.BeginSend(byteData, 0, byteData.Length, 0,  
                    new AsyncCallback(SendCallback), client);  

            } catch {}  
        }  
    
        private void SendCallback(IAsyncResult ar) {  
            try {  
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;  
    
                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);  
    
                // Signal that all bytes have been sent.  
                sendDone.Set();  
            } catch {}
        }  
    }  
}