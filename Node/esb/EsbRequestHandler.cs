using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        private void DeployJob((Job operation, Job shadow) job, string shadow_ignore_node = "", string operation_ignore_node = "")
        {
            if (job.shadow != null && shadow_ignore_node == "")
            {
                shadow_ignore_node = Scheduler.Instance.GetScheduledNode(job.shadow);
            }

            if (operation_ignore_node=="") operation_ignore_node = Scheduler.Instance.GetScheduledNode(job.operation, shadow_ignore_node);
            
            job.operation.CounterPart = (shadow_ignore_node, job.shadow.ID);
            job.shadow.CounterPart = (operation_ignore_node, job.operation.ID);

            bool shadow_status = Scheduler.Instance.ScheduleFinishedJob(shadow_ignore_node, job.shadow);
            bool operation_status = Scheduler.Instance.ScheduleFinishedJob(operation_ignore_node, job.operation);

            if (!shadow_status) DeployJob(job, operation_ignore_node:operation_ignore_node);
            else Logger.Log("ProcessAction", "Processed new shadow job " + job.shadow.ID + ", with counterpart: " + job.shadow.CounterPart.JobId + " on " + job.shadow.CounterPart.Node, Logger.LogLevel.INFO);
            if (!operation_status) DeployJob(job, shadow_ignore_node:shadow_ignore_node);
            else Logger.Log("ProcessAction", "Processed new operatonal job " + job.operation.ID + ", with counterpart: " + job.operation.CounterPart.JobId + " on " + job.operation.CounterPart.Node, Logger.LogLevel.INFO);
        }

        private void ProcessAction(ActionRequest request)
        {
            List<(Job operation, Job shadow)> jobs = HardwareAbstraction.Instance.CreateJobs(request);
            foreach ((Job operation, Job shadow) job in jobs)
            {
                if (job.operation.TypeOfRequest == RequestType.SUBSCRIBE)
                {
                    DeployJob(job);
                } else 
                {
                    Scheduler.Instance.ScheduleJob(job.operation);
                }
            }
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
                                        
                        Response = _client.Run(request.Host, request.Port, request.Name, new RegistrationRequest(){
                            Node = new PlainMetaNode(){
                                ID = Params.UNIQUE_KEY,
                                Name = Params.NODE_NAME,
                                Host = Params.ADVERTISED_HOST_NAME,
                                Port = Params.PORT_NUMBER
                            }
                        }, RequestType.CT);
                        
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
                                Ledger.Instance.AddNode(r0.Node.Name, PlainMetaNode.MakeMetaNode(r0.Node));
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