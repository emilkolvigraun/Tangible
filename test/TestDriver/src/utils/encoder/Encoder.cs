using System;
using Newtonsoft.Json;
using System.Text;  

namespace TangibleDriver 
{
    class Encoder 
    {
        public static byte[] EncodeNode(Node node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static Node DecodeNode(string msg)
        {
            return JsonConvert.DeserializeObject<Node>(msg);
        }
        public static Node DecodeNode(byte[] msg)
        {
            return JsonConvert.DeserializeObject<Node>(Encoding.ASCII.GetString(msg));
        }
        public static byte[] EncodeRequest(Request request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None));
        }
        public static Request DecodeRequest(string msg)
        {
            return JsonConvert.DeserializeObject<Request>(msg);
        }
        public static byte[] EncodeRequestBatch(RequestBatch request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None) + "<EOF>");
        }
        public static RequestBatch DecodeRequestBatch(string msg)
        {
            return JsonConvert.DeserializeObject<RequestBatch>(msg.Replace("<EOF>",""));
        }
        public static byte[] EncodeResponse(Response node)
        {
            try 
            {
                string str = JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>";
                byte[] bytes = Encoding.ASCII.GetBytes(str);
                return Encoding.ASCII.GetBytes(str);
            } catch {return null;}
        }
        public static string SerializeResponse(Response node)
        {
            return JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>";
        }
        public static Response DecodeResponse(string msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<Response>(msg.Replace("<EOF>", ""));
            } catch {return null;}
        }
        public static byte[] EncodePointRequest(PointRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static PointRequest DecodePointRequest(string msg)
        {
            return JsonConvert.DeserializeObject<PointRequest>(msg);
        }
        public static PointRequest DecodePointRequest(byte[] msg)
        {
            return JsonConvert.DeserializeObject<PointRequest>(Encoding.ASCII.GetString(msg)+"<EOF>");
        }
        public static PointResponse DecodePointResponse(byte[] msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<PointResponse>(Encoding.ASCII.GetString(msg).Replace("<EOF>",""));
            } catch 
            {
                return null;
            }
        }
        public static byte[] EncodePointResponse(PointResponse response)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response, Formatting.None));
        }
        public static byte[] EncodePointRequestBatch(PointRequestBatch msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None)+"<EOF>");
        }
        public static PointRequestBatch DecodePointRequestBatch(byte[] msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<PointRequestBatch>(Encoding.ASCII.GetString(msg).Replace("<EOF>",""));
            } catch 
            {
                return null;
            }
        }
        public static PointRequestBatch DecodePointRequestBatch(string msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<PointRequestBatch>(msg.Replace("<EOF>",""));
            } catch {return null;}
        }
        public static byte[] EncodeValueResponse(ValueResponse msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None));
        }
    }
}