using System;

namespace Node 
{
    class NetVariables
    {
        public static string ADVERTISED_HOST_NAME;
        public static string INTERFACE;
        public static int PORT_NUMBER;
        public static string CERTIFICATE = "NODE_CERT.pfx";
        public static int CERT_EXPIRE_DAYS;


        public static void Load()
        {
            string ced = Environment.GetEnvironmentVariable("CERT_EXPIRE_DAYS");
            if (ced == null) CERT_EXPIRE_DAYS = 365; else CERT_EXPIRE_DAYS = int.Parse(ced);
            string pn = Environment.GetEnvironmentVariable("PORT_NUMBER");
            if (pn == null) PORT_NUMBER = NetUtils.GetAvailablePort(); else PORT_NUMBER = int.Parse(pn);
            string inf = Environment.GetEnvironmentVariable("INTERFACE");
            if (inf == null) INTERFACE = "0.0.0.0"; else INTERFACE = inf;
            string ahn = Environment.GetEnvironmentVariable("ADVERTISED_HOST_NAME");
            if (ahn == null) 
            {
                throw new ArgumentException("ADVERTISED_HOST_NAME is undefined.");
            } else ADVERTISED_HOST_NAME = ahn;
        }
    }
}