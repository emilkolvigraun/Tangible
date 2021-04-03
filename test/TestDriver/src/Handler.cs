using System;
using System.Collections.Generic;

namespace TangibleDriver
{

    class ResponseHandler : IResponseHandler
    {
        public void OnResponse(string receiverID, RequestBatch sender, string response)
        {
            try 
            {
                Response r = Encoder.DecodeResponse(response);
                foreach(KeyValuePair<string, bool> status in r.Status)
                {
                    if (!status.Value)
                    {
                        new AsynchronousClient().StartClient(sender, this);
                    }
                    else Logger.Write(Logger.Tag.COMMIT, "Send result.");
                }
            } catch (Exception e)
            {
                Logger.Write(Logger.Tag.ERROR, e.ToString());
            }
        }
    }

    public class Handler
    {

        private AsynchronousClient Client {get;}

        public Handler()
        {
            Client = new AsynchronousClient();
        }

        public void ProcessRequest(Request request)
        {
            try 
            {
                PointRequest pointRequest = Encoder.DecodePointRequest(request.Data);
                request.Data = Encoder.EncodeDriverResponse(
                    new DriverResponse(){
                        ReturnTopic = pointRequest.ReturnTopic,
                        T0 = pointRequest.T0.ToString(),
                        T1 = pointRequest.T1.ToString(),
                        T2 = Utils.Millis.ToString(),
                        Value = pointRequest.Value
                    }
                );
                Logger.Write(Logger.Tag.INFO, "Received request [request:"+pointRequest.ID.Substring(0, 5)+"..., Type:" + pointRequest.Type.ToString() +", Value:" + pointRequest.Value +"]");
                Client.StartClient(new RequestBatch(){
                    Batch = new List<Request>(){
                        request
                    }
                }, new ResponseHandler());
            } catch (Exception e)
            {
                Logger.Write(Logger.Tag.ERROR, e.ToString());
            }
        }
    }
}