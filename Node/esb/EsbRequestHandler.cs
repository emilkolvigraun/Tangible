using System;
using System.Threading.Tasks;

namespace Node 
{
    class EsbRequestHandler
    {
        public void ProcessRequest(string request)
        {
            try 
            {   
                IRequest r0 = request.ParseRequest();
                switch (r0.TypeOf) 
                {
                    case RequestType.READ or RequestType.WRITE or RequestType.SUBSCRIBE 
                        or RequestType.CREATE_ROLE or RequestType.CREATE_USER: 
                            ProcessAction((ActionRequest) r0);
                            break;
                    case RequestType.BC:
                        ProcessBroadcast((BroadcastRequest) r0);
                        break;
                    default:
                        Logger.Log("ProcessRequest", "Unable to parse request.", Logger.LogLevel.WARN);
                        break;
                }
            } catch (Exception e) 
            {
                Logger.Log("ProcessRequest", e.Message, Logger.LogLevel.ERROR);
            }
        }

        private void ProcessAction(ActionRequest request)
        {
            Console.WriteLine(request.EncodeRequestStr());
            Coordinator.Instance.ScheduleJobToNodes(new Job(){
                TypeOf = Job.Type.OP,
                StatusOf = Job.Status.NS,
                Request = request
            });
            // Console.WriteLine("node:" + (n0!=null?n0.Name:Params.NODE_NAME));
        }

        private void ProcessBroadcast(BroadcastRequest request)
        {           
            if (request.Node.Name != Params.NODE_NAME && request.Node.Port != Params.PORT_NUMBER)
            {
                Logger.Log("ProcessBroadcast", "Received BC request from [node:" + request.Node.Name + "] [1]", Logger.LogLevel.INFO);
                IRequest Response = null;
                byte[] _cert_b = null;
                try 
                {
                    Response = NodeClient.RunClient(request.Node.Host, request.Node.Port, request.Node.Name, RequestType.RS, RequestType.CT);
                    if(Response.TypeOf == RequestType.EMPTY) 
                    {
                        return;
                    }
                    _cert_b  = ((CertificateResponse) Response).Cert;

                    // if the connection was successful, add the nodes jobs (it was gossiping)
                    Ledger.Instance.SetNodesJobs(request.Node.Name, ((CertificateResponse)Response).Jobs);
                } catch (Exception e)
                {
                    Logger.Log("ProcessBroadcast", e.Message, Logger.LogLevel.ERROR);
                    return;
                }
                Task.Run(()=>{Params.StoreCertificate(_cert_b);});
                Ledger.Instance.AddNode(request.Node.Name, request.Node);
                Logger.Log("ProcessBroadcast", "Processed BC request from [node:" + request.Node.Name + "] [2]", Logger.LogLevel.INFO);
            }
        }  
    }
}