using System;
using System.Net.Security;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Driver 
{
    static class Extensions
    {
        public static byte[] GetBytes(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        public static string GetString(this byte[] bytes)
        {
            try 
            {
                char[] chars = new char[bytes.Length / sizeof(char)];
                System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                return new string(chars);
            } catch (Exception)
            {
                try 
                {
                    char[] chars = new char[bytes.Length];
                    System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                    return new string(chars);
                } catch(Exception)
                {
                    return null;
                }
            }
        }
        public static byte[] ReadRequest(this SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.
            byte[] b = new byte[2048];
            byte[] cb = null;
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(b, 0, b.Length);
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

        public static byte[] EncodeRequest(this IRequest request)
        {
            return EncodeRequestStr(request).GetBytes();
        }
        public static string EncodeRequestStr(this IRequest request)
        {
            try
            {
                return JsonConvert.SerializeObject(request, Formatting.None);
            } catch(Exception)
            {
                return null;
            }
        }
        public static dynamic DecodeRequest(this byte[] request)
        {
            return request.GetString().DecodeRequest();
        }
        public static dynamic DecodeRequest(this string request)
        {
            try 
            {
                return JsonConvert.DeserializeObject<ExpandoObject>(request);
            } catch (Exception)
            {
                return new EmptyRequest();
            }
        }
        public static RequestType GetRequestType(this string typeOf)
        {
            return (RequestType) Enum.Parse(typeof(RequestType), typeOf);
        }
        public static IRequest ParseRequest(this byte[] request)
        {
            dynamic r0 = request.DecodeRequest();
            string typeOf = r0.TypeOf.ToString();
            return GetRequest(typeOf, request.GetString());         
        }
        public static IRequest ParseRequest(this string request)
        {
            dynamic r0 = request.DecodeRequest();
            string typeOf = r0.TypeOf.ToString();
            return GetRequest(typeOf, request);
        }

        private static IRequest GetRequest(string typeOf, string request)
        {
            try 
            {
                switch (typeOf.GetRequestType())
                {
                    // Intercom requests
                    case RequestType.HI:
                        return JsonConvert.DeserializeObject<Execute>(request);
                    case RequestType.RN:
                        return JsonConvert.DeserializeObject<RunAsRequest>(request);
                    // default
                    default:
                        return JsonConvert.DeserializeObject<EmptyRequest>(request);
                }
            } catch (Exception)
            {
                return new EmptyRequest();
            }
        }
    }
}