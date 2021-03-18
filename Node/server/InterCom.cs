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
                if(Consumer.Instance.IsRunning) Consumer.Instance.Stop();
                Coordinator.Instance.ChangeQueue.EnqueueRange(_add:request.Add, _del:request.Flag);

                // QUEUE NEW JOBS
                if(request.Jobs != null && request.Jobs.Length > 0)
                {
                    Logger.Log("AppendEntry", "Received job.", Logger.LogLevel.IMPOR);
                    Scheduler.Instance.QueueJobs(request.Jobs);
                } 
                Coordinator.Instance.ResetHeartbeat();
            } catch (Exception e)
            {
                Logger.Log("AppendEntry", e.Message, Logger.LogLevel.ERROR);
            }

            return new AppendEntriesRequest(){
                Add = Ledger.Instance.Cluster.AsNodeArray(),
                Flag = null,
                Jobs = Scheduler.Instance.Jobs
                // RETURN ALL MY JOBS
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
                Ledger.Instance.AddNode(request.Node.Name, request.Node);
                // From the registration, a node gossips its current jobs
                // Ledger.Instance.SetNodesJobs(request.Name, request.Jobs);
                if (request.Add != null) Logger.Log("Registration", "Received quorum: " + request.Add.Length.ToString(), Logger.LogLevel.INFO);
                
                // a node also tells about whatever nodes it is connected to
                Coordinator.Instance.ChangeQueue.EnqueueRange(_add:request.Add);

                // and for good measure, the hb is reset
                Coordinator.Instance.ResetHeartbeat();
                Logger.Log("RequestHandler", "Processed RS request from [node:" + request.Node.Name +"]", Logger.LogLevel.INFO);
                return new CertificateResponse().EncodeRequest();
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