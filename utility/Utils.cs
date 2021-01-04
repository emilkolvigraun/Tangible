using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace EC.MS
{
    class Utils 
    {
        internal static readonly char[] chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static string GetUniqueKey(int keySize)
        { 
            byte[] data = new byte[4 * keySize];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            { 
                crypto.GetBytes(data);
            } 
            StringBuilder result = new StringBuilder(keySize);
            for (int i = 0; i < keySize; i++)
            { 
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

        public static List<Uri> GetUrisFromEnvVariable(string servers, string protocol = ""){
            string[] _brokers = servers.Split(",");
            List<Uri> _uris = new List<Uri>();
            foreach(string broker in _brokers)
            {
                if (!string.IsNullOrEmpty(broker)){
                    Uri uri = new Uri(protocol + broker);
                    _uris.Add( uri );
                }
            }
            return _uris;
        }
    }
}