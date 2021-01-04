using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EC.MS
{
    class Client 
    {
        public static string NotifyServer(string request){
            TcpClient Client;
            SslStream Stream;
            try
            {
                Client = new TcpClient(Variables.IP_ADDRESS, Variables.PORT);
                Stream = new SslStream( Client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateWorker), null );
                Stream.AuthenticateAsClient("");
            }
            catch (Exception){return "";} 
 
            byte[] messsage = Encoding.UTF8.GetBytes(Variables.API_KEY + " " + "mon" + " " + request.Replace(" ", "_"));

            Stream.Write(messsage);
            Stream.Flush(); 

            byte[] bytes = ParseBytes(Stream);
            return Encoding.UTF8.GetString(bytes);
        }  

        public static byte[] ParseBytes(SslStream _stream)
        {
            // Read message from the server. 
            byte[] b = new byte[2048];
            byte[] cb = null;
            int bytes = -1;
            do
            {
                bytes = _stream.Read(b, 0, b.Length);
                byte[] rb = b.Take(bytes).ToArray();
                if (cb == null) cb = rb;
                else
                {
                    byte[] tb = new byte[cb.Length + rb.Length];
                    int offset = 0;
                    Buffer.BlockCopy(cb, 0, tb, offset, cb.Length);
                    offset += cb.Length;
                    Buffer.BlockCopy(rb, 0, tb, offset, rb.Length);
                    cb = tb;
                }

                if (bytes < 2048) break;

            } while (bytes != 0);
            return cb;
        }  

        public static bool ValidateWorker(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
            return true;
        }
    }
}