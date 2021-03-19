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
            string id = Utils.GetUniqueKey(size:10);
            Job job = new Job(){
                ID = id,
                TypeOf = Job.Type.OP,
                StatusOf = Job.Status.NS,
                Request = request
            };
            Scheduler.Instance.ScheduleJob(job);
            Logger.Log("ProcessAction", "Processed new job " + job.ID, Logger.LogLevel.IMPOR);
        }

        private void ProcessBroadcast(BroadcastRequest request)
        {           
            if (request.Name != Params.NODE_NAME && request.Port != Params.PORT_NUMBER)
            {
                Logger.Log("ProcessBroadcast", "Received BC request from [node:" + request.Name + "] [1]", Logger.LogLevel.INFO);
                IRequest Response = null;
                byte[] _cert_b = null;
                try 
                {
                    Response = NodeClient.RunClient(request.Host, request.Port, request.Name, RequestType.RS, RequestType.CT);
                    if(Response.TypeOf == RequestType.EMPTY) 
                    {
                        return;
                    }
                    CertificateResponse r0 = ((CertificateResponse) Response);
                    _cert_b  = r0.Cert;
                    Ledger.Instance.AddNode(r0.Node.Name, r0.Node);

                    Task.Run(()=>{Params.StoreCertificate(_cert_b);});
                    Logger.Log("ProcessBroadcast", "Processed BC request from [node:" + request.Name + "] [2]", Logger.LogLevel.INFO);
                    // if the connection was successful, add the nodes jobs (it was gossiping)
                    // Ledger.Instance.SetNodesJobs(request.Node.Name, ((CertificateResponse)Response).Jobs);
                } catch (Exception e)
                {
                    Logger.Log("ProcessBroadcast", e.Message, Logger.LogLevel.ERROR);
                    return;
                }
            }
        }  
    }
}