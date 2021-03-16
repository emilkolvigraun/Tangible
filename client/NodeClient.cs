using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Node
{
    public class NodeClient
    {
        public static IRequest RunClient(string host, int port, string name, RequestType request, RequestType result = RequestType.NIL, int __retries = 0)
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
                return RunClient(host, port, name, request, result, __retries+1);
            }
            return Response;
        } 
        public static IRequest RunClient(string host, int port, string name, IRequest request, RequestType result = RequestType.NIL, int __retries = 0, bool timeout = false)
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
                return RunClient(host, port, name, request, result, __retries+1, timeout);
            }
            return Response;
        } 

        private static IRequest WriteRequest(string machineName, int port, string targetHost, IRequest request, bool timeout = false)
        {
            IRequest response = new EmptyRequest();

            TcpClient client = null;
            try
            {
                // Create a TCP/IP client socket.
                // machineName is the host running the server application.
                if (!timeout) client = new TcpClient(machineName,port);
                else {
                    client = new TcpClient();
                    if (!client.ConnectAsync(machineName,port).Wait(150))
                    {
                        Logger.Log("WriteRequest", "Unable to reach follower - " + machineName + ":" + port.ToString(), Logger.LogLevel.WARN);
                        client.Close();
                        return response;
                    }
                }
                Logger.Log("NodeClient", "Client connected to " +targetHost, Logger.LogLevel.DEBUG);
                // Create an SSL stream that will close the client's stream.
                SslStream sslStream = new SslStream(
                    client.GetStream(),
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
                    client.Close();
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
                client.Close();      
            } catch (Exception e)
            {
                Logger.Log("WriteRequest", e.Message, Logger.LogLevel.ERROR);
            }
            try 
            {
                if(client != null) client.Close();
            }catch(Exception){}
            
            return response;
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