using System;

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

        private void ProcessBroadcast(BroadcastRequest request)
        {           
            if (!request.Name.Equals(Params.NODE_NAME) && !request.Port.Equals(Params.PORT_NUMBER))
            {
                Logger.Log("ProcessBroadcast", "Received BC request from " + request.Name + "[1]", Logger.LogLevel.INFO);
                IRequest Response = null;
                byte[] _cert_b = null;
                try 
                {
                    Response = NodeClient.RunClient(request.Host, request.Port, request.Name, RequestType.RS, RequestType.CT);
                    if(Response.TypeOf == RequestType.EMPTY) 
                    {
                        return;
                    }
                    _cert_b  = ((CertificateResponse) Response).Cert;
                } catch (Exception e)
                {
                    Logger.Log("ProcessBroadcast", e.Message, Logger.LogLevel.ERROR);
                    return;
                }
                Params.StoreCertificate(_cert_b);
                Ledger.Instance.AddNode(request.Name, Builder.CreateMetaNode(request.Name, request.Host, request.Port));
                Logger.Log("ProcessBroadcast", "Processed BC request from " + request.Name + "[2]", Logger.LogLevel.INFO);
            }
        }  
    }
}