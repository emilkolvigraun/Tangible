using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Threading;  
using System.Text;
using System.Collections.Generic;  

namespace TangibleNode 
{
    public class AsynchronousClient {          
        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);  
        private ManualResetEvent sendDone =
            new ManualResetEvent(false);  
        private ManualResetEvent receiveDone =
            new ManualResetEvent(false);  

        public string Host {get;}
        public int Port {get;}
        public string ID {get;}

        private bool _notified {get; set;} = false;

        public AsynchronousClient(string host, int port, string id)
        {
            Host = host;
            Port = port;
            ID = id;
        }
    
        // The response from the remote device.  
        private String response = String.Empty;  
    
        /// <summary>
        /// Establishes connection to the remote host, sends the request an activates OnResponse on the requesthandler
        /// </summary>
        public void StartClient(RequestBatch request, IResponseHandler rh) {  
            // Connect to a remote device.  
            try {  
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(Host), Port);  
    
                // Create a TCP/IP socket.  
                Socket client = new Socket(AddressFamily.InterNetwork,  
                    SocketType.Stream, ProtocolType.Tcp);  

                // Connect to the remote endpoint.  
                IAsyncResult result = client.BeginConnect( remoteEP,
                    new AsyncCallback(ConnectCallback), client);  

                connectDone.WaitOne(((int)(Params.HEARTBEAT_MS/2)), true);  
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
                    rh.OnResponse(ID, request, response);  
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
            } catch
            {  
                // Logger.Write(Logger.Tag.ERROR, e.ToString());
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
            rh.OnResponse(ID, request, Encoder.SerializeResponse(new Response(){
                Status = r0
            }));  
            
            Logger.Write(Logger.Tag.WARN, "Unable to connect to [node:" + ID +"][retries:"+StateLog.Instance.Peers.GetHeartbeat(ID)+"]");
            StateLog.Instance.Peers.AccessHeartbeat(ID, (hb) => {hb.Increment();});   
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