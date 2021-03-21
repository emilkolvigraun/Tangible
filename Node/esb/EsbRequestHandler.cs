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
            Logger.Log("ProcessAction", "Processed new job " + job.ID, Logger.LogLevel.INFO);
        }

        private void ProcessBroadcast(BroadcastRequest request)
        {       
            try 
            {
                if (request.Name != Params.NODE_NAME && request.Port != Params.PORT_NUMBER)
                {
                    IRequest Response = null;
                    byte[] _cert_b = null;
                    try 
                    {
                        NodeClient _client = null;
                        try 
                        {
                            _client = Coordinator.Instance.GetClient(request.Name); 
                        } catch(Exception e)
                        {
                            Logger.Log("ProcessBroadcast", "[2] " + e.Message, Logger.LogLevel.ERROR);
                            return;
                        }
                                        
                        Response = _client.Run(request.Host, request.Port, request.Name, RequestType.RS, RequestType.CT);
                        
                        if(Response == null || Response.TypeOf != RequestType.CT) 
                        {
                            Logger.Log("ProcessBroadcast", "Something went wrong while processing BC", Logger.LogLevel.WARN);
                            return;
                        } else 
                        {
                            CertificateResponse r0 = null;
                            try 
                            {
                                r0 = ((CertificateResponse) Response);
                                _cert_b  = r0.Cert;
                                Ledger.Instance.AddNode(r0.Node.Name, r0.Node);
                                Task.Run(()=>{Params.StoreCertificate(_cert_b);});
                                Logger.Log("ProcessBroadcast", "Processed BC request from [node:" + request.Name + ", id:"+r0.Node.ID+"]", Logger.LogLevel.INFO);
                            } catch (Exception e)
                            {
                                Logger.Log("ProcessBroadcast", "[3] " + (_cert_b==null) + " " + (r0==null) + " " +  e.Message, Logger.LogLevel.ERROR);
                                return;
                            }
                        }
                    } catch (Exception e)
                    {
                        Logger.Log("ProcessBroadcast", "[1] " + e.Message, Logger.LogLevel.ERROR);
                        return;
                    }
                }
            } catch (Exception e)
            {
                Logger.Log("ProcessBroadcast", "[0] " + e.Message, Logger.LogLevel.ERROR);
            }    
        }  
    }
}