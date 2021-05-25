using System;

namespace TangibleNode
{
    public class PrintHandler : IResponseHandler 
    {
        public void OnResponse(string receiverID, ProcedureCallBatch sender, string response)
        {
            Console.WriteLine(response);
            Response r0 = Encoder.DecodeResponse(response);
            // Console.WriteLine(request.Type);
        }
    }
}