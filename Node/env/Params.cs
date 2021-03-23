using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Node
{
    class Params 
    {
        // boot variable
        public static int WAIT_TIME_MS;
        public static int TIMEOUT_LIMIT;
        public static string UNIQUE_KEY;

        // kafka variables
        public static string KAFKA_BROKERS;
        public static string CLUSTER_ID;
        public static string BROADCAST_TOPIC;
        public static string REQUEST_TOPIC;

        // coordination variables
        public static int HEARTBEAT_MS;

        // net variables
        public static string ADVERTISED_HOST_NAME;
        public static int PORT_NUMBER;
        public static X509Certificate X509CERT;
        public static byte[] X509CERT_BYTES;
        
        // tangible variables
        public static string NODE_NAME;
        public static double USAGE;

        // docker variables
        public static string DOCKER_HOST_NAME;


        public static void LoadConfig()
        {
            WAIT_TIME_MS        = GetIntVar("WAIT_TIME_MS", 0);
            KAFKA_BROKERS       = GetStrVar("KAFKA_BROKERS");
            CLUSTER_ID          = GetStrVar("CLUSTER_ID");
            BROADCAST_TOPIC     = GetStrVar("BROADCAST_TOPIC");
            REQUEST_TOPIC       = GetStrVar("REQUEST_TOPIC");
            ADVERTISED_HOST_NAME= GetStrVar("ADVERTISED_HOST_NAME");
            PORT_NUMBER         = GetIntVar("PORT_NUMBER");
            TIMEOUT_LIMIT       = GetIntVar("TIMEOUT_LIMIT", alternative:30, minimum:20);
            HEARTBEAT_MS        = Utils.GetRandomInt(300, 500);
            UNIQUE_KEY          = Utils.GetUniqueKey(size: 10);
            NODE_NAME           = GetStrVar("NODE_NAME", Utils.GetUniqueKey(size:10));
            DOCKER_HOST_NAME    = GetStrVar("DOCKER_HOST_NAME");

            string CERT_NAME    = NODE_NAME+".pfx";
            X509CERT_BYTES = GenerateCertificate(NODE_NAME);
            File.WriteAllBytes(CERT_NAME , X509CERT_BYTES);
            StoreCertificate(X509CERT_BYTES);
            X509CERT            = X509Certificate.CreateFromCertFile(CERT_NAME);
            File.Delete(CERT_NAME);
            Logger.LoadLogLevel();
            USAGE               = Utils.ResourceUsage;
        }

        private static string GetStrVar(string var, string alterntive = "")
        {
            string v = Environment.GetEnvironmentVariable(var);
            if (v == null) 
            {
                if (string.IsNullOrEmpty(alterntive)) throw new ArgumentException(var +" is undefined.");
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
    }
}