﻿using System;
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
            Running = true;
            Server.Start();
            AwaitClient(); 
        }  

        public void Stop()
        {  
            Running = false;
            Server.EndAcceptTcpClient(null);
            Server.Stop();
        } 

        private void AwaitClient() 
        { 
            Server.BeginAcceptTcpClient(ClientHandler, Server);
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
                    RequestHandler.Instance.ProcessFromType(ParsedRequest, sslStream);
                } catch (Exception e)
                {
                    Logger.Log(this.GetType().Name, "parsing " + e.Message);
                }
            }
            catch (Exception e)
            {
                Logger.Log(this.GetType().Name, e.Message); 
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