using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Collections;

namespace Driver
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

        public NodeClient ()
        {
            Host = Params.NODE_HOST;
            Port = Params.NODE_PORT;
            Name = Params.NODE_NAME;
        }

        private static Hashtable certificateErrors = new Hashtable();

        public IRequest Run(IRequest request)
        {
            lock(_lock)
            {
                IRequest response = new EmptyRequest();
                try 
                {
                    // Create a TCP/IP client socket.
                    // machineName is the host running the server application.
                    TcpClient client = new TcpClient(Host, Port);
                    // if (!client.ConnectAsync(Host, Port).Wait(500)) return response;
                    // Console.WriteLine("Client connected.");
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
                        response = sslStream.ReadRequest().ParseRequest();
                    } catch (Exception e)
                    {Console.WriteLine(e.Message);}
                    
                    // Close the client connection.
                    client.Close();

                } catch(Exception e)
                {Console.WriteLine(e.Message);}
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