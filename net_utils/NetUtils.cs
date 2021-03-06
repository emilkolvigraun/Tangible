using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net;
using System.Net.NetworkInformation;

namespace Node
{
    class NetUtils
    {

        ///<summary>
        ///<para>Generates and store a new (self-signed) certificate, if that certificate does not already exist.</para>
        ///<returns>void</returns>
        ///</summary>
        public static void MakeCertificate() 
        {
            File.WriteAllBytes(NetVariables.CERTIFICATE, GenerateCertificate(Utils.GetUniqueKey(size:5)));
            StoreCertificate(NetVariables.CERTIFICATE);
        }

        public static X509Certificate2 LoadCertificate()
        {
            return new X509Certificate2(X509Certificate2.CreateFromCertFile(NetVariables.CERTIFICATE));
        }

        public static string LoadCommonName()
        {          
            return AsyncTLSServer.Instance.serverCertificate.Subject.ToString().Replace("CN=","");
        }

        public static byte[] GenerateCertificate(string CN)
        {
            using var rsa = RSA.Create();
            var certRequest = new CertificateRequest("cn="+CN, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificate = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(NetVariables.CERT_EXPIRE_DAYS));
            return certificate.Export(X509ContentType.Pkcs12);
        }

        public static bool StoreCertificate(byte[] byteArr)
        {
            X509Certificate2 _cert = new X509Certificate2(byteArr);
            return StoreCertificate(_cert);
        }

        public static bool StoreCertificate(string FileName)
        {
            X509Certificate2 _cert = new X509Certificate2(X509Certificate2.CreateFromCertFile(FileName));
            return StoreCertificate(_cert);
        }

        public static bool StoreCertificate(X509Certificate2 _cert)
        { 
            try
            {
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(_cert);
                    store.Close();
                }
                return true;
            }
            catch (Exception) 
            {
                return false;
            }
        }

        public static string DecodeRequest(SslStream _stream)
        {
            return Utils.GetString(ParseBytes(_stream));
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

        public static string IPAddress 
        {
            get
            {  
                String address = "";  
                WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");  
                using (WebResponse response = request.GetResponse())  
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))  
                {  
                    address = stream.ReadToEnd();  
                }  
            
                int first = address.IndexOf("Address: ") + 9;  
                int last = address.LastIndexOf("</body>");  
                address = address.Substring(first, last - first);  
            
                return address;  
            } 
        }

        public static int GetAvailablePort(int startingPort = 8000)
        {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                                                .Concat(udpListenersEndpoints)
                                                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }

        public static void SendString(SslStream stream, string message)
        {  
            SendBytes(stream, Utils.GetBytes(message));
        } 

        public static void SendBytes(SslStream stream, byte[] bytes)
        { 
            stream.Write(bytes);
            stream.Flush();
        }

        public static void SendRequestResponse(SslStream stream, Request request)
        {
            SendString(stream, RequestUtils.SerializeRequest(request));
        }
    }
}