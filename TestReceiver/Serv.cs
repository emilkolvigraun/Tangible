using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Server
{
    public sealed class Serv
    {
        static X509Certificate serverCertificate = null;
        static InterCom Handler = new InterCom();
        public static void RunServer()
        {
            serverCertificate = Params.X509CERT;
            TcpListener listener = new TcpListener(IPAddress.Any, 5006);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ProcessClient(client);
            }
        }
        static void ProcessClient (TcpClient client)
        {
            // A client has connected. Create the
            // SslStream using the client's network stream.
            SslStream sslStream = new SslStream(
                client.GetStream(), false);
            // Authenticate the server but don't require the client to authenticate.
            try
            {
                sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                // Set timeouts for the read and write to 5 seconds.
                sslStream.ReadTimeout = 5000;
                sslStream.WriteTimeout = 5000;
                // Read a message from the client.
                byte[] response = Handler.ProcessRequest(sslStream.ReadRequest());

                // Write a message to the client.
                // byte[] message = Encoding.UTF8.GetBytes("Hello from the server.<EOF>");
                Console.WriteLine("RESPONSE: " + response.ParseRequest().EncodeRequestStr());
                sslStream.Write(response);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine ("Authentication failed - closing the connection.");
                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                // The client stream will be closed with the sslStream
                // because we specified this behavior when creating
                // the sslStream.
                sslStream.Close();
                client.Close();
            }
        }
    }
}