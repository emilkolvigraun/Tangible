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
            Console.WriteLine(request.EncodeRequestStr());
            return new RequestResponse(){
                ID = request.ID,
                T0 = request.T0,
                T1 = Params.Millis,
                Value = request.Value
            }.EncodeRequest();
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