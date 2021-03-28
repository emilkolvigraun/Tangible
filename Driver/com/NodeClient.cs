using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Driver
{
    public class NodeClient
    {

        private readonly object _lock = new object();
        private TcpClient _Client = new TcpClient();
        LingerOption _lingerOption = new LingerOption(false, 1);

        public IRequest WriteRequest(string host, int port, string targetHost, IRequest request, bool timeout = false)
        {
            IRequest response = new EmptyRequest();
            ReInstanceClient();
            if(!_Client.ConnectAsync(host, port).Wait(500))
            {
                return response;
            }
            
            // Create an SSL stream that will close the client's stream.
            SslStream sslStream = new SslStream(
                _Client.GetStream(),
                false,
                new RemoteCertificateValidationCallback (ValidateServerCertificate),
                null
                );
                
            // The server name must match the name on the server certificate.
            try
            {
                sslStream.AuthenticateAsClient(targetHost);
            }
            catch (AuthenticationException e)
            {
                sslStream.Close();
                return response;
            }
            
            // encoding request
            sslStream.Write(request.EncodeRequest());
            sslStream.Flush();

            // Read message from the server.
            try 
            {
                response = sslStream.ReadRequest().ParseRequest();
            } catch (Exception)
            {
            }
                
            // Close the client connection.
            sslStream.Close();

            return response;
        }
        
        private void ReInstanceClient()
        {
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