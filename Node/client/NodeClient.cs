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
        LingerOption _lingerOption = new LingerOption(false, 1);

        public IRequest Run(string host, int port, string name, RequestType request, RequestType result = RequestType.NIL, int __retries = 0)
        {
            lock(_lock)
            {
                try 
                {
                    IRequest Response = WriteRequest(host, port, name, request.GetCreateRequest()); 
                    if (result == RequestType.NIL) return Response;
                    else if (__retries > 2 && result != Response.TypeOf)
                    {
                        Logger.Log("RunClient", "BC request responded with EMPTY type", Logger.LogLevel.ERROR);
                        return Response;
                    } 
                    else if (result != Response.TypeOf) 
                    {
                        Logger.Log("RunClient", name + " did not return expected request type - retrying: " + (__retries+1).ToString(), Logger.LogLevel.WARN);
                        return Run(host, port, name, request, result, __retries+1);
                    }
                    return Response;
                } catch (Exception e)
                {
                    Logger.Log("RunClient", e.Message, Logger.LogLevel.ERROR);
                    return new EmptyRequest();
                }
            }
        } 

        public IRequest Run(string host, int port, string name, IRequest request, RequestType result = RequestType.NIL, int __retries = 0, bool timeout = false)
        {
            lock(_lock)
            {
                IRequest Response = WriteRequest(host, port, name, request, timeout); 
                if (result == RequestType.NIL) return Response;
                else if (__retries > 2 && result != Response.TypeOf)
                {
                    Logger.Log("RunClient", "BC request responded with EMPTY type", Logger.LogLevel.ERROR);
                    return Response;
                } 
                else if (result != Response.TypeOf) 
                {
                    Logger.Log("RunClient", name + " did not return expected request type - retrying: " + (__retries+1).ToString(), Logger.LogLevel.WARN);
                    return Run(host, port, name, request, result, __retries+1, timeout);
                }
                return Response;
            }
        }

        private IRequest WriteRequest(string host, int port, string targetHost, IRequest request, bool timeout = false)
        {
            IRequest response = new EmptyRequest();
            ReInstanceClient();
            if(!_Client.ConnectAsync(host, port).Wait(150))
            {
                Logger.Log("WriteRequest", "Unable to reach follower - " + host + ":" + port.ToString(), Logger.LogLevel.WARN);
                return response;
            }

            Logger.Log("NodeClient", "Client connected to " +targetHost, Logger.LogLevel.DEBUG);
            
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
                response = sslStream.ReadRequest().ParseRequest();
            } catch (Exception e)
            {
                Logger.Log("WriteRequest", e.Message, Logger.LogLevel.ERROR);
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