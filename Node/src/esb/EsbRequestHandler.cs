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
                IRequest r0 = request.DecodeRequest();
                switch (r0.TypeOf) 
                {
                    case RequestType.READ or RequestType.WRITE or RequestType.SUBSCRIBE: 
                        ProcessAction((ActionRequest) r0);
                        break;
                    case RequestType.BROADCAST:
                        ProcessBroadcast((BroadcastRequest) r0);
                        break;
                    default:
                        Logger.Log("ProcessRequest", "Unable to parse request.", Logger.LogLevel.WARN);
                        break;
                }
            } catch (Exception e) 
            {
                Logger.Log("ESBHandler", "ProcessRequest, " + e.Message, Logger.LogLevel.ERROR);
            }
        }

        private void ProcessAction(ActionRequest request)
        {
            try 
            {
                request.ID = Utils.GetUniqueKey();
                request.Timestamp = Utils.Millis;
                PriorityQueue.Instance.Enqueue(request);
            } catch(Exception e)
            {
                Logger.Log("ProcessAction", e.Message, Logger.LogLevel.ERROR);
            }
        }

        private void ProcessBroadcast(BroadcastRequest request)
        {       
            try 
            {
                if (request._Node.Name != Params.NODE_NAME && request._Node.Port != Params.PORT_NUMBER
                    && request._Node.Key != Params.UNIQUE_KEY)
                {
                    IRequest Response = null;
                    byte[] _cert_b = null;
                    try 
                    {
                        NodeClient _client = null;
                        try 
                        {
                            _client = Cluster.Instance.AddGetClient(request._Node); 
                            Ledger.Instance.InitClient(request._Node.Key);
                        } catch(Exception e)
                        {
                            Logger.Log("ProcessBroadcast", "[2] " + e.Message, Logger.LogLevel.ERROR);
                            return;
                        }
                                        
                        Response = _client.Run(new RegistrationRequest(){
                            _Node = new Node(){
                                Name = Params.NODE_NAME,
                                Host = Params.ADVERTISED_HOST_NAME,
                                Port = Params.PORT_NUMBER,
                                Key  = Params.UNIQUE_KEY
                            }
                        });
                        
                        if(Response == null || Response.TypeOf != RequestType.CERTIFICATE) 
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
                                Task.Run(()=>{Params.StoreCertificate(_cert_b);});
                                Logger.Log("ProcessBroadcast", "Processed BC request from [node:" + request._Node.Name + ", id:"+request._Node.Key+"]", Logger.LogLevel.INFO);
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