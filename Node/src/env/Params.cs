using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Generic;
using System.Linq;


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

        public static string DRIVER_ADVERTISED_HOST_NAME;
        public static int DRIVER_RANGE_START;
        public static int DRIVER_RANGE_END;
        public static int MAX_PRIORITY;
        public static int BATCH_SIZE;
        public static int CONNECT_TIMEOUT;


        // docker variables
        public static string DOCKER_ADVERTISED_HOST_NAME;
        public static string HIE_ADVERTISED_HOST_NAME;

        public static string DOCKER_USER;
        public static string DOCKER_PASSWORD;
        public static string DOCKER_EMAIL;


        public static void LoadConfig()
        {

            string dr = Environment.GetEnvironmentVariable("DRIVER_RANGE");
            if (dr == null)
            {
                DRIVER_RANGE_START = 6000;
                DRIVER_RANGE_END = 7000;
            } else {
                string[] rng = dr.Split("->");
                DRIVER_RANGE_START = int.Parse(rng[0]);
                DRIVER_RANGE_END = int.Parse(rng[1]);
            }

            // Using ports between DRIVER_RANGE_START and DRIVER_RANGE_END
            for(int i = DRIVER_RANGE_START; i < DRIVER_RANGE_END; i++)
            {
                UnusedPorts.Add(i);
            }

            WAIT_TIME_MS        = GetIntVar("WAIT_TIME_MS", 0, 0);
            KAFKA_BROKERS       = GetStrVar("KAFKA_BROKERS");
            CLUSTER_ID          = GetStrVar("CLUSTER_ID");
            BROADCAST_TOPIC     = GetStrVar("BROADCAST_TOPIC");
            REQUEST_TOPIC       = GetStrVar("REQUEST_TOPIC");
            ADVERTISED_HOST_NAME= GetStrVar("ADVERTISED_HOST_NAME");
            DRIVER_ADVERTISED_HOST_NAME= GetStrVar("DRIVER_ADVERTISED_HOST_NAME", ADVERTISED_HOST_NAME);
            PORT_NUMBER         = GetIntVar("PORT_NUMBER");
            MAX_PRIORITY         = GetIntVar("MAX_PRIORITY", alternative:3, minimum:0);
            BATCH_SIZE       = GetIntVar("BATCH_SIZE", alternative:5, minimum:1);
            CONNECT_TIMEOUT       = GetIntVar("CONNECT_TIMEOUT", alternative:150, minimum:100);
            TIMEOUT_LIMIT       = GetIntVar("TIMEOUT_LIMIT", alternative:30, minimum:20);
            HEARTBEAT_MS        = Utils.GetRandomInt(300, 500);
            UNIQUE_KEY          = Utils.GetUniqueKey();
            NODE_NAME           = GetStrVar("NODE_NAME", UNIQUE_KEY);
            DOCKER_ADVERTISED_HOST_NAME    = GetStrVar("DOCKER_ADVERTISED_HOST_NAME");
            HIE_ADVERTISED_HOST_NAME       = GetStrVar("HIE_ADVERTISED_HOST_NAME", Params.ADVERTISED_HOST_NAME);
            DOCKER_EMAIL        = GetStrVar("DOCKER_EMAIL", null);
            DOCKER_USER         = GetStrVar("DOCKER_USER", null);
            DOCKER_PASSWORD     = GetStrVar("DOCKER_PASSWORD", null);

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

        private static readonly object _port_lock = new object();
        private static List<int> UnusedPorts = new List<int>();
        public static int UNUSED_PORT 
        {
            get 
            {
                if (UnusedPorts.Count == 0)
                {
                    Logger.Log("UNUSED_PORT", "No more unused ports!", Logger.LogLevel.FATAL);
                    return -1;
                }
                int length = UnusedPorts.Count;
                int port = UnusedPorts.ElementAt(0);
                UnusedPorts.RemoveAt(0);
                if (length == UnusedPorts.Count) Logger.Log("UNUSED_PORT", "Did not find an unused port", Logger.LogLevel.FATAL);
                return port;
            }
        }
    }
}