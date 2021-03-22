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
                Coordinator.Instance.ResetHeartbeat();
                Coordinator.Instance.StopElectionTerm();
                Coordinator.Instance.SetCurrentLeader(request.Name);
                if(Coordinator.Instance.IsLeader) Coordinator.Instance.ToggleLeadership(false);
                if(Consumer.Instance.IsRunning) Consumer.Instance.Stop();
                if(request.Jobs.Length > 0)
                {
                    Scheduler.Instance.UpdateJobs(request.Jobs);
                } 
                if(request.Nodes.Length > 0 || request.Remove.Length > 0 || request.Ledger.Length > 0)
                {
                    Ledger.Instance.UpdateNodes(request.Ledger, request.Nodes, request.Remove);
                }
                // Coordinator.Instance.ResetHeartbeat();
            } catch (Exception e)
            {
                Logger.Log("AppendEntry", e.Message, Logger.LogLevel.ERROR);
            }

            return new AppendEntriesResponse(){
                // NodeIds = Ledger.Instance.Cluster.GetIds(),
                JobIds = Scheduler.Instance._Jobs.GetIds(),
                Ledger = Ledger.Instance.Cluster.GetLedger()
                }.EncodeRequest();
        }
        private byte[] ProcessRegistration(RegistrationRequest request)
        {
            try 
            {
                Task.Run(()=>{Params.StoreCertificate(request.Cert);});
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS [0]: " + e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
            try 
            {
                Ledger.Instance.AddNode(request.Node.Name, PlainMetaNode.MakeMetaNode(request.Node));

                // and for good measure, the hb is reset
                Coordinator.Instance.ResetHeartbeat();
                Logger.Log("RequestHandler", "Processed RS request from [node:" + request.Node.Name +", id:" + request.Node.ID +"]", Logger.LogLevel.INFO);
                return new CertificateResponse(){
                    Node = new PlainMetaNode(){
                        Name = Params.NODE_NAME,
                        ID = Params.UNIQUE_KEY,
                        Host = Params.ADVERTISED_HOST_NAME,
                        Port = Params.PORT_NUMBER
                    }
                }.EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS [1]: " + e.Message, Logger.LogLevel.ERROR);
                return new CertificateResponse().EncodeRequest();
            }
        }
    
        private byte[] ProcessVoting(VotingRequest request)
        {
            try 
            {
                Coordinator.Instance.ResetHeartbeat();
                Coordinator.Instance.StopElectionTerm();
                Coordinator.Instance.ToggleLeadership(false);
                string _vote = "";
                if (Coordinator.Instance.IsElectionTerm)
                {
                    if (Ledger.Instance.ClusterCopy.Count - 1 < 2) _vote = request.Vote;
                    else _vote = Params.NODE_NAME;
                    Logger.Log(this.GetType().Name, "Received VT request and stopped election term.", Logger.LogLevel.INFO);
                } 
                else 
                {
                    _vote = request.Vote;
                    Logger.Log(this.GetType().Name, "Received VT request [from:" + request.Vote + "]", Logger.LogLevel.INFO);
                }
                Coordinator.Instance.ResetHeartbeat();
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