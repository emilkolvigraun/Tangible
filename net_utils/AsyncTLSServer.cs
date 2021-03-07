using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Node
{
    /// <summary>
    /// SSLServer create an SSL TCP Server. 
    /// <para>_certificate : specifies the path to the 
    /// certificate, string.</para>
    /// </summary>
    class AsyncTLSServer : IDisposable
    {
        public X509Certificate serverCertificate {get; private set;}
        private TcpListener Server;
        private bool Running; 

        AsyncTLSServer() 
        { 
            Running = false;
            Server = new TcpListener(IPAddress.Parse(NetVariables.INTERFACE), NetVariables.PORT_NUMBER);
            serverCertificate = X509Certificate.CreateFromCertFile(NetVariables.CERTIFICATE); 
        } 

        public byte[] GetEncodedCertificate()
        {
            return File.ReadAllBytes(NetVariables.CERTIFICATE);
        }

        public void AsyncStart() 
        { 
            Logger.Log(this.GetType().Name, "Starting Server", Logger.LogLevel.INFO);
            Running = true;
            Server.Start();
            AwaitClient(); 
        }  

        public void Stop()
        {  
            try 
            {
                Running = false;
                Server.EndAcceptTcpClient(null);
                Server.Stop();
            } catch (Exception) { }
        } 

        private void AwaitClient() 
        { 
            Server.BeginAcceptTcpClient(ClientHandler, Server);
            Logger.Log(this.GetType().Name, "Awaiting next client", Logger.LogLevel.INFO);
        } 

        private void ClientHandler(IAsyncResult result)
        { 
            if (!Running) return;

            var _listener = result.AsyncState as TcpListener;
            var _client = Server.EndAcceptTcpClient(result);

            AwaitClient();  

            SslStream sslStream = new SslStream(_client.GetStream(), false);
            try
            {


                sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
                
                try {
                    Request ParsedRequest = RequestUtils.DeserializeRequest(sslStream);
                    sslStream.ReadTimeout = 10000;
                    sslStream.WriteTimeout = 10000;
                    if (ParsedRequest != null) RequestHandler.Instance.ProcessFromType(ParsedRequest, sslStream);
                    else {
                        Logger.Log(this.GetType().Name, "Received malformed request.", Logger.LogLevel.INFO);
                        // Orchestrator.Instance.Broadcast();
                    }
                } catch (Exception e)
                {
                    sslStream.Close();
                    _client.Close();
                    Logger.Log(this.GetType().Name, "Error parsing " + e.Message, Logger.LogLevel.ERROR);
                }
            }
            catch (Exception e)
            {
                Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR); 
                sslStream.Close();
                _client.Close();
                return;
            }
            finally
            { 
                sslStream.Close();
                _client.Close();
            }
        }   

        public void Dispose() 
        {
            Stop();
        }
        private static AsyncTLSServer _instance = null;
        private static readonly object padlock = new object();
        public static AsyncTLSServer Instance
        {
            get {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new AsyncTLSServer();
                    }
                    return _instance;
                }
            }
        }
    }
}
