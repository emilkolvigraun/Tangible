using System;
using Newtonsoft.Json;
using System.Text;  

namespace TangibleDriver 
{
    class Encoder 
    {
        public static byte[] EncodeNode(Credentials node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static Credentials DecodeNode(string msg)
        {
            return JsonConvert.DeserializeObject<Credentials>(msg);
        }
        public static Credentials DecodeNode(byte[] msg)
        {
            return JsonConvert.DeserializeObject<Credentials>(Encoding.ASCII.GetString(msg));
        }
        public static byte[] EncodeCall(Call request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None));
        }
        public static Call DecodeCall(string msg)
        {
            return JsonConvert.DeserializeObject<Call>(msg);
        }
        public static byte[] EncodeProcedureCallBatch(ProcedureCallBatch request)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(request, Formatting.None) + "<EOF>");
        }
        public static ProcedureCallBatch DecodeProcedureCallBatch(string msg)
        {
            return JsonConvert.DeserializeObject<ProcedureCallBatch>(msg.Replace("<EOF>",""));
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
        public static byte[] EncodePointRequest(DataRequest node)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(node, Formatting.None));
        }
        public static DataRequest DecodePointRequest(string msg)
        {
            return JsonConvert.DeserializeObject<DataRequest>(msg);
        }
        public static byte[] EncodeValueResponseBatch(ValueResponseBatch batch)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(batch, Formatting.None));
        }

        public static DataRequest DecodePointRequest(byte[] msg)
        {
            return JsonConvert.DeserializeObject<DataRequest>(Encoding.ASCII.GetString(msg)+"<EOF>");
        }
        public static StatusResponse DecodePointResponse(byte[] msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<StatusResponse>(Encoding.ASCII.GetString(msg).Replace("<EOF>",""));
            } catch 
            {
                return null;
            }
        }
        public static byte[] EncodePointResponse(StatusResponse response)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response, Formatting.None));
        }
        public static byte[] EncodeDataRequestBatch(DataRequestBatch msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None)+"<EOF>");
        }
        public static DataRequestBatch DecodeDataRequestBatch(byte[] msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<DataRequestBatch>(Encoding.ASCII.GetString(msg).Replace("<EOF>",""));
            } catch 
            {
                return null;
            }
        }
        public static DataRequestBatch DecodeDataRequestBatch(string msg)
        {
            try 
            {
                return JsonConvert.DeserializeObject<DataRequestBatch>(msg.Replace("<EOF>",""));
            } catch {return null;}
        }
        public static byte[] EncodeValueResponse(ValueResponse msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None));
        }

        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None);
        }
    }
}