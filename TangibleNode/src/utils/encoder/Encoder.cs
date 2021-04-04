using System;
using Newtonsoft.Json;
using System.Text;  

namespace TangibleNode 
{
    class Encoder 
    {
        public static byte[] EncodeAction(Action action)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(action, Formatting.None));
        }
        public static Action DecodeAction(string msg)
        {
            return JsonConvert.DeserializeObject<Action>(msg);
        }
        public static Action DecodeAction(byte[] msg)
        {
            return JsonConvert.DeserializeObject<Action>(Encoding.ASCII.GetString(msg));
        }
        public static byte[] EncodeVote(Vote node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static Vote DecodeVote(byte[] msg)
        {
            return JsonConvert.DeserializeObject<Vote>(Encoding.ASCII.GetString(msg));
        }
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
            string str = JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>";
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            return Encoding.ASCII.GetBytes(str);
        }
        public static string SerializeResponse(Response node)
        {
            return JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>";
        }
        public static Response DecodeResponse(string msg)
        {
            return JsonConvert.DeserializeObject<Response>(msg.Replace("<EOF>", ""));
        }
        public static byte[] EncodeESBRequest(ESBRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>");
        }
        public static ESBRequest DecodeESBRequest(string msg)
        {
            return JsonConvert.DeserializeObject<ESBRequest>(msg.Replace("<EOF>", ""));
        }
        public static byte[] EncodeDataRequest(DataRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static DataRequest DecodeDataRequest(string msg)
        {
            return JsonConvert.DeserializeObject<DataRequest>(msg);
        }
        public static byte[] EncodeBroadcast(Broadcast node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static DriverResponse DecodeDriverResponse(string msg)
        {
            return JsonConvert.DeserializeObject<DriverResponse>(msg);
        }
        public static DriverResponse DecodeDriverResponse(byte[] msg)
        {
            return JsonConvert.DeserializeObject<DriverResponse>(Encoding.ASCII.GetString(msg));
        }
        public static Broadcast DecodeBroadcast(string msg)
        {
            return JsonConvert.DeserializeObject<Broadcast>(msg);
        }
        public static byte[] EncodeRequestResponse(RequestResponse request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None));
        }
        public static byte[] EncodeDriverResponse(DriverResponse node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
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
            return JsonConvert.DeserializeObject<PointRequest>(Encoding.ASCII.GetString(msg));
        }
        
    }
}