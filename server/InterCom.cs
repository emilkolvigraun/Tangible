using System;
using System.Collections.Generic;
using System.Linq;

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
            try
            {
                Logger.Log("RequestHandler", "Received AE request", Logger.LogLevel.INFO);
                Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
                Coordinator.Instance.Cancel();
                Coordinator.Instance.ToggleLeadership(request.Name == Params.NODE_NAME, request.Name);
                List<MetaNode> _add = request.Add.ToList();
                List<string> _flag = request.Flag.ToList();
                foreach(MetaNode n0 in _add)
                {
                    if (n0.Name != Params.NODE_NAME && !Ledger.Instance.ContainsKey(n0.Name)
                        && !Coordinator.Instance.FlaggedCopy.Contains(n0.Name) && !_flag.Contains(n0.Name))
                    {
                        Ledger.Instance.AddNode(n0.Name, n0);
                    }
                }
                foreach(string n0 in _flag)
                {
                    if (n0 != Params.NODE_NAME && !Coordinator.Instance.FlaggedCopy.Contains(n0))
                    {
                        Coordinator.Instance.AddFlag(n0);
                    }
                }
                List<MetaNode> _nodes = new List<MetaNode>();
                foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
                {
                    if (!_add.Contains(n0.Value) && !_flag.Contains(n0.Key) && Coordinator.Instance.FlaggedCopy.Contains(n0.Key)) _nodes.Add(n0.Value);
                }
                Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
                Coordinator.Instance.Cancel();

                if (_nodes.Count > 0) return new AppendEntriesRequest(){
                    Add = _nodes.ToArray(),
                    Flag = null
                }.EncodeRequest();

            } catch(Exception e)
            {
                Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR);
            }

            return new AppendEntriesRequest(){
                Add = null,
                Flag = null
            }.EncodeRequest();
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