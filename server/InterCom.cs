using System;
using System.Collections.Generic;
using System.Linq;
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
                Coordinator.Instance.StopElectionTerm();
                Coordinator.Instance.SetCurrentLeader(request.Name);
                if(Coordinator.Instance.IsLeader) Coordinator.Instance.ToggleLeadership(false);
                Coordinator.Instance.EnqueueRange(_add:request.Add, _del:request.Flag);
                Coordinator.Instance.ResetHeartbeat();
            } catch (Exception e)
            {
                Logger.Log("AppendEntry", e.Message, Logger.LogLevel.ERROR);
            }

            return new AppendEntriesRequest(){
                Add = Ledger.Instance.ClusterCopy.AsNodeArray(),
                Flag = null
            }.EncodeRequest();
        }
        private byte[] ProcessRegistration(RegistrationRequest request)
        {
            try 
            {
                Task.Run(()=>{Params.StoreCertificate(request.Cert);});
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS[0]: " + e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
            try 
            {
                Ledger.Instance.AddNode(request.Name, new MetaNode(){
                        Name = request.Name, 
                        Host = request.Host,
                        Port = request.Port
                    });
                if (request.Add != null) Logger.Log("Registration", "Received quorum: " + request.Add.Length.ToString(), Logger.LogLevel.INFO);
                Coordinator.Instance.EnqueueRange(_add:request.Add);
                Coordinator.Instance.ResetHeartbeat();
                Logger.Log("RequestHandler", "Processed RS request from " + request.Name, Logger.LogLevel.INFO);
                return new CertificateResponse().EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS[1]: " + e.Message, Logger.LogLevel.ERROR);
                return new CertificateResponse().EncodeRequest();
            }
        }
    
        private byte[] ProcessVoting(VotingRequest request)
        {
            try 
            {
                Coordinator.Instance.ResetHeartbeat();
                string _vote = "";
                if (Coordinator.Instance.IsElectionTerm)
                {
                    if (Ledger.Instance.ClusterCopy.Count - 1 < 2) _vote = request.Vote;
                    else _vote = Params.NODE_NAME;
                    Coordinator.Instance.StopElectionTerm();
                    Coordinator.Instance.ToggleLeadership(false);
                    Logger.Log(this.GetType().Name, "Received VT request and stopped election term.", Logger.LogLevel.INFO);
                } 
                else 
                {
                    _vote = request.Vote;
                    Logger.Log(this.GetType().Name, "Received VT request [from:" + request.Vote + "]", Logger.LogLevel.INFO);
                }
                return new VotingRequest(){
                    Vote = _vote
                }.EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("ProcessVoting", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
        }
    }
}