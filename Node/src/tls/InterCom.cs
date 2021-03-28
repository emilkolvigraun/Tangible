using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Node 
{
    class InterCom
    {
        public byte[] ProcessRequest(IRequest request)
        {
            switch(request.TypeOf)
            {
                case RequestType.REGISTRATION:
                    return ProcessRegistration((RegistrationRequest)request);
                case RequestType.VOTE:
                    return ProcessVote((VotingRequest)request);
                case RequestType.APPEND_REQ:
                    return ProcessAppendEntry((AppendEntryRequest)request);
                case RequestType.RESPONSE:
                    return ProcessResponse((RequestResponse)request);
                default:
                    return new EmptyRequest().EncodeRequest();
            }
        }

        private byte[] ProcessResponse(RequestResponse request)
        {
            ResponseQueue.Instance.Add(request);
            return new StatusResponse(){Status = true}.EncodeRequest();
        }

        private byte[] ProcessAppendEntry(AppendEntryRequest request)
        {
            CurrentState.Instance.ResetHeartbeat();
            CurrentState.Instance.SetIsLeader(false);
            CurrentState.Instance.StopElectionTerm();
            
            // node attach
            foreach(Node client in request.NodeAttach)
            {
                Cluster.Instance.AddClient(client);
                Ledger.Instance.InitClient(client.Key);
            }

            // append
            foreach(KeyValuePair<string, List<Request>> append in request.LogAppend)
            {
                foreach(Request r0 in append.Value)
                    Ledger.Instance.AddRequest(append.Key, r0);
            }
            
            // detach
            foreach(KeyValuePair<string, List<string>> detach in request.LogDetach)
            {
                foreach(string r0 in detach.Value)
                    Ledger.Instance.RemoveRequest(detach.Key, r0);
            }
            
            // enqueue request
            foreach(Request r0 in request.Enqueue)
            {
                RequestQueue.Instance.Enqueue(r0);
            }

            // dequeue request
            foreach(string r0 in request.Dequeue)
            {
                RequestQueue.Instance.DetachRequest(r0);
            }
            
            // node detach
            foreach(string n0 in request.NodeDetach)
            {
                Ledger.Instance.RemoveClient(n0);
                Cluster.Instance.RemoveClient(n0);
            }

            foreach(ActionRequest acr in request.PriorityAttach)
            {
                PriorityQueue.Instance.Enqueue(acr);
            }

            PriorityQueue.Instance.DetachRequest(request.PriorityDetach);

            return new AppendEntryResponse(){
                Status = true,
                Completed = RequestQueue.Instance.CompletedRequests
            }.EncodeRequest();
        }

        private byte[] ProcessVote(VotingRequest request)
        {
            try 
            {
                CurrentState.Instance.ResetHeartbeat();
                CurrentState.Instance.StopElectionTerm();
                CurrentState.Instance.SetIsLeader(false);
                string _vote = "";
                if (CurrentState.Instance.IsElectionTerm)
                {
                    if (Cluster.Instance.Count - 1 < 2) _vote = request.Vote;
                    else _vote = Params.NODE_NAME;
                    Logger.Log(this.GetType().Name, "Received VT request and stopped election term.", Logger.LogLevel.INFO);
                } 
                else 
                {
                    _vote = request.Vote;
                    Logger.Log(this.GetType().Name, "Received VT request [from:" + request.Vote + "]", Logger.LogLevel.INFO);
                }
                CurrentState.Instance.ResetHeartbeat();
                return new VotingRequest(){
                    Vote = _vote
                }.EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("ProcessVoting", e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
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
                // and for good measure, the hb is reset
                CurrentState.Instance.InitResetHeartbeat();
                Cluster.Instance.AddClient(request._Node);
                Ledger.Instance.InitClient(request._Node.Key);
                CurrentState.Instance.InitResetHeartbeat();

                Logger.Log("RequestHandler", "Processed RS request from [node:" + request._Node.Name +", id:" + request._Node.Key +"]", Logger.LogLevel.INFO);
                
                return new CertificateResponse().EncodeRequest();
            } catch (Exception e)
            {
                Logger.Log("RequestHandler", "RS [1]: " + e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
        }
    }
}