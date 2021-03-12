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
                    case RequestType.BC:
                        return JsonConvert.DeserializeObject<BroadcastRequest>(request);
                    default:
                        return JsonConvert.DeserializeObject<EmptyRequest>(request);
                }
            } catch (Exception e)
            {
                Logger.Log("GetRequest", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest();
            }
        }

        public static bool RemoveNode(this KeyValuePair<string, MetaNode> node)
        {
            return Ledger.Instance.Cluster.TryRemove(node);
        }
    
        public static BasicNode[] AsBasicNodes(this ConcurrentDictionary<string, MetaNode> nodes)
        {
            List<BasicNode> basicNodes = new List<BasicNode>();
            foreach(KeyValuePair<string, MetaNode> node in nodes)
            {   
                basicNodes.Add(node.Value.AsBasicNode());
            }
            return basicNodes.ToArray();
        }

        public static BasicNode AsBasicNode(this INode node)
        {
            return new BasicNode(){
                Host = node.Host,
                Port = node.Port,
                Name = node.Name
            };
        }

        public static string[] AsStringArray(this BasicNode[] nodes)
        {
            List<string> strArr = new List<string>();
            foreach (BasicNode node in nodes)
            {
                strArr.Add(
                    "{"+node.Name+","+node.Host+":"+node.Port.ToString()+"}"
                );
            }
            return strArr.ToArray();
        }

        public static IRequest GetCreateRequest(this RequestType _type)
        {
            switch (_type)
            {
                case RequestType.RS: return new RegistrationRequest();
                case RequestType.AE: return new AppendEntriesRequest();
                case RequestType.BC: return new BroadcastRequest();
                case RequestType.CT: return new CertificateResponse();
                default: return new EmptyRequest();
            }
        }

        public static int AsCeilInt(this double v0)
        {
            return (int) Math.Ceiling((decimal) v0);
        }
    }
}