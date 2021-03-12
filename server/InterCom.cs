using System;
using System.Threading.Tasks;

namespace Node 
{
    class InterCom
    {
        public byte[] ProcessRequest(byte[] byteArr)
        {
            IRequest request = byteArr.ParseRequest();
            switch (request.TypeOf)
            {
                case RequestType.RS:
                    return ProcessRegistration((RegistrationRequest)request);
                case RequestType.AE:
                    return ProcessAppendEntry((AppendEntriesRequest)request);
                default:
                    Logger.Log("RequestHandler", "Received malformed request", Logger.LogLevel.DEBUG);
                    return new EmptyRequest().EncodeRequest();
            }
        }

        private byte[] ProcessAppendEntry(AppendEntriesRequest request)
        {
            Logger.Log("RequestHandler", "Received AE request", Logger.LogLevel.DEBUG);
            return new EmptyRequest().EncodeRequest();
        }
        private byte[] ProcessRegistration(RegistrationRequest request)
        {
            try 
            {
                Params.StoreCertificate(request.Cert);
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS: " + e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }

            foreach (BasicNode node in request.Nodes)
            {
                if (!node.Name.Equals(Params.NODE_NAME) && !node.Port.Equals(Params.PORT_NUMBER))
                {
                    Console.WriteLine(Params.NODE_NAME + " != " + node.Name);
                    CertificateResponse Response = null;
                    Response = (CertificateResponse) NodeClient.RunClient(node.Host, node.Port, node.Name, RequestType.RS, RequestType.CT);
                    if(Response.TypeOf != RequestType.EMPTY) 
                    {
                        Params.StoreCertificate(Response.Cert);
                        Ledger.Instance.AddNode(node.Name, Builder.CreateMetaNode(node.Name, node.Host, node.Port, Response.Usage, Response.Nodes));
                        Logger.Log("ProcessBroadcast", "Registered+ " + node.Name, Logger.LogLevel.INFO);
                    }
                }
            }

            Ledger.Instance.AddNode(request.Name, Builder.CreateMetaNode(request.Name, request.Host, request.Port, request.Usage, request.Nodes));
            Logger.Log("RequestHandler", "Received RS request from " + request.Name, Logger.LogLevel.INFO);
            return new CertificateResponse().EncodeRequest();
        }
    
        private byte[] ProcessVoting(VotingRequest request)
        {
            return new EmptyRequest().EncodeRequest();
        }
        private byte[] ProcessConnectNodes(VotingRequest request)
        {
            return new EmptyRequest().EncodeRequest();
        }
    }
}