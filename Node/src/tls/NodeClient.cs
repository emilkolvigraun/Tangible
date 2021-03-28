using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;


namespace Node
{
    public class NodeClient
    {

        private readonly object _lock = new object();
        private TcpClient _Client = new TcpClient();
        SslStream sslStream = null;
        LingerOption _lingerOption = new LingerOption(false, 1);

        private string Host;
        private int Port;
        private string Name;

        public NodeClient (string host, string name, int port)
        {
            Host = host;
            Port = port;
            Name = name;
        }

        public IRequest Run(IRequest request)
        {
            lock(_lock)
            {
                IRequest response = new EmptyRequest();
                ReinstantiateClient();
                if(!_Client.ConnectAsync(Host, Port).Wait(Params.CONNECT_TIMEOUT))
                {
                    Logger.Log("WriteRequest", "Unable to reach server - " + Host + ":" + Port.ToString(), Logger.LogLevel.WARN);
                    return response;
                }

                Logger.Log("NodeClient", "Client connected to " + Name, Logger.LogLevel.DEBUG);
                
                // Create an SSL stream that will close the client's stream.
                sslStream = new SslStream(
                    _Client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback (ValidateServerCertificate),
                    null
                    );
                    
                // The server name must match the name on the server certificate.
                try
                {
                    sslStream.AuthenticateAsClient(Name);
                }
                catch (AuthenticationException e)
                {
                    Logger.Log("WriteRequest",  e.Message, Logger.LogLevel.ERROR);
                    if (e.InnerException != null)
                    {
                        Logger.Log("WriteRequest",  e.InnerException.Message, Logger.LogLevel.ERROR);
                    }
                    sslStream.Close();
                    return response;
                }
                
                // encoding request
                sslStream.Write(request.EncodeRequest());
                sslStream.Flush();

                // Read message from the server.
                try 
                {
                    response = sslStream.ReadRequest();
                } catch (Exception e)
                {
                    Logger.Log("WriteRequest", e.Message, Logger.LogLevel.ERROR);
                }
                    
                // Close the client connection.
                sslStream.Close();

                return response;
            }
        }
        
        private void ReinstantiateClient()
        {
            try 
            {
                if(sslStream != null)
                    sslStream.Close();
            }catch(Exception){}
            
            if (_Client != null)
            {
                _Client.Close();
            }
            _Client = new TcpClient();
            _Client.Client.LingerState = _lingerOption;
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}