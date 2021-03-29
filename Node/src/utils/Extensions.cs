using System;
using System.Net.Security;
using System.Linq;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.NetworkInformation;

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
        public static IRequest ReadRequest(this SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.

            byte[] b = new byte[2048];
            byte[] cb = null;
            try 
            {
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
            } catch (Exception e)
            {
                Logger.Log("ReadRequest", e.Message + "\n" + e.StackTrace, Logger.LogLevel.ERROR);
            }

            return cb.GetString().DecodeRequest();
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
        public static string Serialize(this Response request)
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
        public static IRequest DecodeRequest(this string request)
        {
            try 
            { 
                dynamic r = JsonConvert.DeserializeObject<ExpandoObject>(request);
                string typeOf = r.TypeOf.ToString();
                switch(typeOf.GetRequestType())
                {
                    case RequestType.BROADCAST:
                        return JsonConvert.DeserializeObject<BroadcastRequest>(request);
                    case RequestType.REGISTRATION:
                        return JsonConvert.DeserializeObject<RegistrationRequest>(request);
                    case RequestType.CERTIFICATE:
                        return JsonConvert.DeserializeObject<CertificateResponse>(request);
                    case RequestType.VOTE:
                        return JsonConvert.DeserializeObject<VotingRequest>(request);
                    case RequestType.APPEND_REQ:
                        return JsonConvert.DeserializeObject<AppendEntryRequest>(request);
                    case RequestType.APPEND_RES:
                        return JsonConvert.DeserializeObject<AppendEntryResponse>(request);
                    case RequestType.RESPONSE:
                        return JsonConvert.DeserializeObject<RequestResponse>(request);
                    case RequestType.STATUS:
                        return JsonConvert.DeserializeObject<StatusResponse>(request);
                    case RequestType.EXECUTE:
                        return JsonConvert.DeserializeObject<DriverRequest>(request);
                    case RequestType.READ or RequestType.WRITE or RequestType.SUBSCRIBE:
                        return JsonConvert.DeserializeObject<ActionRequest>(request);
                    default:
                        return new EmptyRequest();
                }
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
    }
}