using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Server 
{
    class Params 
    {
        public static int PORT_NUMBER;
        public static string HOST_NAME;
        public static string MACHINE_NAME;

        public static X509Certificate X509CERT;
        public static byte[] X509CERT_BYTES;

    
        public static void LoadParams()
        {
            PORT_NUMBER = GetIntVar("PORT");
            HOST_NAME = GetStrVar("HOST");
            MACHINE_NAME = GetStrVar("NAME");
            string CERT_NAME    = MACHINE_NAME+".pfx";
            X509CERT_BYTES = GenerateCertificate(MACHINE_NAME);
            File.WriteAllBytes(CERT_NAME , X509CERT_BYTES);
            StoreCertificate(X509CERT_BYTES);
            X509CERT            = X509Certificate.CreateFromCertFile(CERT_NAME);
            File.Delete(CERT_NAME);
        }

        private static string GetStrVar(string var, string alterntive = "")
        {
            string v = Environment.GetEnvironmentVariable(var);
            if (v == null) 
            {
                if (alterntive == "") throw new ArgumentException(var +" is undefined.");
                else return alterntive;
            }
            
            return v;
        }
        private static int GetIntVar(string var, int alternative = -1, int minimum = -1)
        {
            string v = Environment.GetEnvironmentVariable(var);
            if (v == null) 
            {
                if (alternative == -1) throw new ArgumentException(var + " is undefined.");
                else return alternative;
            }
            else if (minimum > -1){
                int i = int.Parse(v);
                if (i < minimum) return minimum;
            } 
            return int.Parse(v);
        }

        private static byte[] GenerateCertificate(string CN)
        {
            using var rsa = RSA.Create();
            var certRequest = new CertificateRequest("cn="+CN, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificate = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(365));
            return certificate.Export(X509ContentType.Pkcs12);
        }
        public static bool StoreCertificate(byte[] byteArr)
        {
            X509Certificate2 _cert = new X509Certificate2(byteArr);
            return StoreCertificate(_cert);
        }
        private static bool StoreCertificate(X509Certificate2 _cert)
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

        /// <summary>Class to get current timestamp with enough precision</summary>
        private static readonly DateTime Jan1St1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
    
    }
}