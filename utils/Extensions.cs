using System;
using System.Net.Security;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Node
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
                } catch(Exception e)
                {
                    Logger.Log("Utils", "GetString, "+e.Message, Logger.LogLevel.ERROR);
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
            } catch(Exception e)
            {
                Logger.Log("EncodeRequest", e.Message, Logger.LogLevel.ERROR);
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
            } catch (Exception e)
            {
                Logger.Log("DecodeRequest", e.Message, Logger.LogLevel.ERROR);
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
                    case RequestType.AE:
                        return JsonConvert.DeserializeObject<AppendEntriesRequest>(request);
                    case RequestType.RS:
                        return JsonConvert.DeserializeObject<RegistrationRequest>(request);
                    case RequestType.CT:
                        return JsonConvert.DeserializeObject<CertificateResponse>(request);
                    case RequestType.VT:
                        return JsonConvert.DeserializeObject<VotingRequest>(request);
                    case RequestType.BC:
                        return JsonConvert.DeserializeObject<BroadcastRequest>(request);
                    case RequestType.ST:
                        return JsonConvert.DeserializeObject<StatusResponse>(request);
                    default:
                        return JsonConvert.DeserializeObject<EmptyRequest>(request);
                }
            } catch (Exception e)
            {
                Logger.Log("GetRequest", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest();
            }
        }
    
        public static IRequest GetCreateRequest(this RequestType _type)
        {
            switch (_type)
            {
                case RequestType.RS: return new RegistrationRequest();
                case RequestType.AE: return new AppendEntriesRequest();
                case RequestType.BC: return new BroadcastRequest();
                case RequestType.CT: return new CertificateResponse();
                case RequestType.ST: return new StatusResponse();
                case RequestType.VT: return new VotingRequest(){Vote = Ledger.Instance.Vote};
                default: return new EmptyRequest();
            }
        }

        public static Dictionary<string, MetaNode> Copy(this Dictionary<string, MetaNode> d0)
        {
            return d0.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        public static MetaNode[] AsNodeArray(this Dictionary<string, MetaNode> d0)
        {
            return d0.Values.ToArray();
        }

        public static string GetMajority(this List<string> votes)
        {
            // throws exception if length of votes is less than 1
            return votes.GroupBy( i => i ).OrderByDescending(group => group.Count()).ElementAt(0).Key;
        }
    }
}