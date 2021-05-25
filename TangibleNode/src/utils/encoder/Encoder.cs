using System;
using Newtonsoft.Json;
using System.Text;  

namespace TangibleNode 
{
    class Encoder 
    {
        public static byte[] EncodeDataRequest(DataRequest action)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(action, Formatting.None));
        }
        public static DataRequest DecodeDataRequest(string msg)
        {
            return JsonConvert.DeserializeObject<DataRequest>(msg);
        }
        public static DataRequest DecodeDataRequest(byte[] msg)
        {
            return JsonConvert.DeserializeObject<DataRequest>(Encoding.ASCII.GetString(msg));
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
        public static byte[] EncodeProcedureCallBatch(ProcedureCallBatch request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None) + "<EOF>");
        }
        public static ProcedureCallBatch DecodeProcedureCallBatch(string msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<ProcedureCallBatch>(msg.Replace("<EOF>",""));
            } catch {return null;}
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
        public static byte[] EncodeESBRequest(ESBRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None) + "<EOF>");
        }
        public static ESBRequest DecodeESBRequest(string msg)
        {
            return JsonConvert.DeserializeObject<ESBRequest>(msg.Replace("<EOF>", ""));
        }
        public static byte[] EncodeESBDataRequest(ESBDataRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static ESBDataRequest DecodeESBDataRequest(string msg)
        {
            return JsonConvert.DeserializeObject<ESBDataRequest>(msg);
        }
        public static byte[] EncodeBroadcast(Broadcast node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static Broadcast DecodeBroadcast(string msg)
        {
            return JsonConvert.DeserializeObject<Broadcast>(msg);
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
        public static PointResponse DecodePointResponse(string msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<PointResponse>(msg.Replace("<EOF>",""));
            } catch 
            {
                return null;
            }
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
        public static ValueResponse DecodeValueResponse(byte[] msg)
        {
            return JsonConvert.DeserializeObject<ValueResponse>(Encoding.ASCII.GetString(msg));
        }
        public static byte[] EncodeRequestResponse(RequestResponse msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None)+"<EOF>");
        }
    }
}