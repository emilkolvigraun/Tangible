using Newtonsoft.Json;
using System.Net.Security;
using Confluent.Kafka;
using System;

namespace Node
{
    class RequestUtils 
    {
        public static Request DeserializeRequest(SslStream Stream)
        {   
            try {
                return JsonConvert.DeserializeObject<Request>(NetUtils.DecodeRequest(Stream));
            } catch(Exception e) {
                Logger.Log("DeserializeRequest", e.Message, Logger.LogLevel.ERROR);
                return null;
            }
        }

        public static Request DeserializeRequest(ConsumeResult<Ignore, string> Request)
        {
            try {
                return JsonConvert.DeserializeObject<Request>(Request.Message.Value.ToString());
            } catch(Exception e) {
                Logger.Log("DeserializeRequest", e.Message, Logger.LogLevel.ERROR);
                return null;
            }
        }

        public static Request DeserializeRequest(string Request)
        {
            try {
                return JsonConvert.DeserializeObject<Request>(Request);
            } catch(Exception e) {
                Logger.Log("DeserializeRequest", e.Message, Logger.LogLevel.ERROR);
                return null;
            }
        }

        public static string SerializeRequest(Request request)
        {
            try {
                return JsonConvert.SerializeObject(request, Formatting.None);
            } catch(Exception e) {
                Logger.Log("SerializeRequest", e.Message, Logger.LogLevel.ERROR);
                return null;
            }
        }
    }
}