using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Driver 
{
    class InterCom
    {
        public byte[] ProcessRequest(byte[] byteArr)
        {
            IRequest request = byteArr.ParseRequest();
            switch (request.TypeOf)
            {
                case RequestType.EXECUTE:
                    return ProcessNextJob((DriverRequest)request);
                // case RequestType.RN:
                //     return ProcessRunAs((RunAsRequest)request);
                default:
                    return new EmptyRequest().EncodeRequest();
            }
        }

        public byte[] ProcessNextJob(DriverRequest request)
        {
            if (request.Action == ActionType.WRITE && request.Value == null)
            {
                return new StatusResponse(){Status = false}.EncodeRequest();
            }
            ProcessQueue.Instance.Enqueue(request);
            return new StatusResponse(){Status = true}.EncodeRequest();
        }

        // public byte[] ProcessRunAs(RunAsRequest request)
        // {
        //     if (request.JobID == null)
        //         return new StatusResponse(){Status = false}.EncodeRequest();

        //     ProcessQueue.Instance.Enqueue(request);
            
        //     return new StatusResponse(){Status = true}.EncodeRequest();
        // }
    }
}