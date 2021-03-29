using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
// using System.Security.Authentication;

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
        private static Hashtable certificateErrors = new Hashtable();

        public IRequest Run(IRequest request)
        {
            lock(_lock)
            {
                IRequest response = new EmptyRequest();

                
                // Create a TCP/IP client socket.
                // machineName is the host running the server application.
                using (TcpClient client = new TcpClient())
                {
                    if (!client.ConnectAsync(Host, Port).Wait(Params.CONNECT_TIMEOUT)) 
                    {
                        try
                        {
                            client.Close();
                        }catch(Exception) {}
                        return response;
                    }
                    // Console.WriteLine("Client connected.");
                    // Create an SSL stream that will close the client's stream.
                    SslStream sslStream = new SslStream(
                        client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback (ValidateServerCertificate),
                        null
                        );
                    
                    try 
                    {
                        try
                        {
                            sslStream.AuthenticateAsClient(Name, null, SslProtocols.Tls|SslProtocols.Tls11|SslProtocols.Tls12|SslProtocols.Tls13, false);
                        }
                        catch (AuthenticationException e)
                        {
                            Console.WriteLine("Exception: {0}", e.Message);
                            if (e.InnerException != null)
                            {
                                Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                            }
                            Console.WriteLine ("Authentication failed - closing the connection.");
                            client.Close();
                            return response;
                        }

                        try 
                        {
                            // Send hello message to the server.
                            sslStream.Write(request.EncodeRequest());
                            sslStream.Flush();
                            // Read message from the server.
                            response = sslStream.ReadRequest();
                        } catch (Exception e)
                        {
                            Logger.Log("Transport", e.Message, Logger.LogLevel.ERROR);
                        }
                        
                        // Close the client connection.
                        sslStream.Flush();
                        sslStream.Close();
                        client.Close();
                        client.Dispose();
                    } catch(Exception)
                    {
                        sslStream.Flush();
                        sslStream.Close();
                        client.Close();
                        client.Dispose();
                        return response;
                    }
                    
                }

                return response;
            }
            
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