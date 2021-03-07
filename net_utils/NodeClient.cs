using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Node
{
    class NodeClient
    {
        private TcpClient Client;
        private SslStream Stream;
        
        public static NodeClient Connect(string Address, int Port, string TargetHost)
        {
            NodeClient TClient = new NodeClient();
            try
            {
                TClient.Client = new TcpClient(Address, Port);
                TClient.Stream = new SslStream
                    (
                        TClient.Client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate),
                        null
                    );

                TClient.Stream.AuthenticateAsClient(TargetHost);
                return TClient;
            }
            catch (Exception)
            {
                try
                {
                    TClient.Client.Close();
                    TClient.Stream.Close();
                }
                catch (NullReferenceException){}
            }
            return null;
        }

        public Request SendRequestRespondRequest(Request request)
        {
            string rq0 = RequestUtils.SerializeRequest(request);
            byte[] byteArr = Utils.GetBytes(rq0);
            Stream.Write(byteArr);
            Stream.Flush();
            Request Response = RequestUtils.DeserializeRequest(Stream);
            Stream.Close();
            Client.Close();
            return Response;
        }

        public byte[] SendRequestRespondBytes(Request request)
        {
            try {
                string SerializedRequest = RequestUtils.SerializeRequest(request);
                byte[] byteArr = Utils.GetBytes(SerializedRequest);
                Stream.Write(byteArr);
                Stream.Flush();
                byte[] Response = NetUtils.ParseBytes(Stream);
                Stream.Close();
                Client.Close();
                return Response;
            } catch (Exception e) {
                Logger.Log(this.GetType().Name, e.Message);
                return new byte[]{};
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // X509Store userCaStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            // try
            // {
            //     userCaStore.Open(OpenFlags.ReadOnly);
            //     X509Certificate2Collection certificatesInStore = userCaStore.Certificates;
            //     foreach (X509Certificate2 cert in certificatesInStore)
            //         if (cert.Issuer.Contains(certificate.Issuer)  
            //             && cert.GetCertHashString().Contains(certificate.GetCertHashString()) 
            //             && cert.Subject.Contains(certificate.Subject)){
            //             return true;
            //         } 
            // } 
            // finally
            // { 
            //     userCaStore.Close();
            // }

            // if (sslPolicyErrors == SslPolicyErrors.None)
            //     return true;

            // return false;

            return true;
        }
    }
}
