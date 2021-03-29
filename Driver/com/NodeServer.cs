using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Driver 
{
    public sealed class NodeServer
    {
        static X509Certificate serverCertificate = null;
        static InterCom Handler = new InterCom();

        // The certificate parameter specifies the name of the file
        // containing the machine certificate.
        public static void RunServer()
        {
            serverCertificate = Params.X509CERT;
            // Create a TCP/IP (IPv4) socket and listen for incoming connections.
            TcpListener listener = new TcpListener(IPAddress.Any, Params.PORT_NUMBER);
            listener.Server.ReceiveTimeout = 5000;
            listener.Server.SendTimeout = 5000;
            listener.Start();
            Console.WriteLine("Running...");
            while (true)
            {
                // Application blocks while waiting for an incoming connection.
                // Type CNTL-C to terminate the server.
                try 
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ProcessClient(client);
                    Console.WriteLine("ready for next client");
                } catch (Exception e)
                {Console.WriteLine("ERROR: " + e.Message);}
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

                // Set timeouts for the read and write to 200 ms.
                sslStream.ReadTimeout = 5000;
                sslStream.WriteTimeout = 5000;

                // Read a message from the client.
                try
                {
                    byte[] request = sslStream.ReadRequest();
                    Console.WriteLine("RECEIVED:" + request.GetString());
                    byte[] response = Handler.ProcessRequest(request);
                    sslStream.Write(response);
                    Console.WriteLine("RESPONDED:" + response.GetString());
                    sslStream.Flush();
                } catch(Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                    sslStream.Close();
                    client.Close();
                }
            }
            catch (AuthenticationException)
            {
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