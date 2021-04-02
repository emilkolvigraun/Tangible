using System;

namespace TangibleNode
{
    public class PrintHandler : IResponseHandler 
    {
        public void OnResponse(string receiverID, RequestBatch sender, string response)
        {
            Request request = Encoder.DecodeRequest(response);
            Console.WriteLine(request.Type);
        }
    }
}