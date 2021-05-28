using System.Collections.Generic;

namespace TangibleDriver 
{
    public interface IRequestHandler 
    {

        void ProcessWrite(WriteRequest writeRequest);
        void ProcessRead(ReadRequest writeRequest);
        void ProcessSubscribe(SubscribeRequest writeRequest);
        void ProcessSubscribeStop(SubscribeRequest writeRequest);
        ValueResponseBatch[] GetResponses();
    }
}