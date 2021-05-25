using System;
using Newtonsoft.Json;
using System.Text;  

namespace TestReceiver 
{
    class Encoder 
    {
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
        public static byte[] EncodePointResponse(PointResponse msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None)+"<EOF>");
        }
        public static byte[] EncodeRequestResponse(RequestResponse msg)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, Formatting.None)+"<EOF>");
        }
        public static RequestResponse DecodeRequestResponse(string msg)
        {
            return JsonConvert.DeserializeObject<RequestResponse>(msg.Replace("<EOF>",""));
        }
    }
}