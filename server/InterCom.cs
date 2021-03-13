using System;
using System.Collections.Generic;

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
                case RequestType.VT:
                    return ProcessVoting((VotingRequest)request);
                default:
                    Logger.Log("RequestHandler", "Received malformed request", Logger.LogLevel.ERROR);
                    return new EmptyRequest().EncodeRequest();
            }
        }

        private byte[] ProcessAppendEntry(AppendEntriesRequest request)
        {
            Logger.Log("RequestHandler", "Received AE request", Logger.LogLevel.DEBUG);
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
            foreach(MetaNode n0 in request.Add)
            {
                if (n0.Name != Params.NODE_NAME && !Ledger.Instance.ContainsKey(n0.Name)
                    && !Coordinator.Instance.FlaggedCopy.Contains(n0.Name))
                {
                    Ledger.Instance.AddNode(n0.Name, n0);
                }
            }
            foreach(string n0 in request.Flag)
            {
                if (n0 != Params.NODE_NAME && !Coordinator.Instance.FlaggedCopy.Contains(n0))
                {
                    Coordinator.Instance.AddFlag(n0);
                }
            }
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
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
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
            Ledger.Instance.AddNode(request.Name, Builder.CreateMetaNode(request.Name, request.Host, request.Port));
            Logger.Log("RequestHandler", "Processed RS request from " + request.Name, Logger.LogLevel.INFO);
            return new CertificateResponse().EncodeRequest();
        }
    
        private byte[] ProcessVoting(VotingRequest request)
        {
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
            Coordinator.Instance.Cancel();
            try 
            {
                Logger.Log(this.GetType().Name, "Received VT request", Logger.LogLevel.INFO);
                Coordinator.Instance.ToggleLeadership(request.Vote == Params.NODE_NAME, request.Vote);
                return new VotingRequest(){
                    Vote = request.Vote
                }.EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("ProcessVoting", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
        }
    }
}