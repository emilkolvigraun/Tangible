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
            Coordinator.Instance.StopElectionTerm();
            Coordinator.Instance.SetCurrentLeader(request.Name);
            if(Coordinator.Instance.IsLeader) Coordinator.Instance.ToggleLeadership(false);

            Coordinator.Instance.EnqueueRange(_add:request.Add, _del:request.Flag);
            Coordinator.Instance.ResetHeartbeat();

            return new AppendEntriesRequest(){
                Add = null,
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
                Logger.Log("RequestHandler", "RS: " + e.Message, Logger.LogLevel.ERROR);
                return new EmptyRequest().EncodeRequest();
            }
            Ledger.Instance.AddNode(request.Name, new MetaNode(){
                    Name = request.Name, 
                    Host = request.Host,
                    Port = request.Port
                });
            Coordinator.Instance.EnqueueRange(_add:request.Add);
            Coordinator.Instance.ResetHeartbeat();
            Logger.Log("RequestHandler", "Processed RS request from " + request.Name, Logger.LogLevel.INFO);
            return new CertificateResponse().EncodeRequest();
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




// try
            // {
            //     Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
            //     Coordinator.Instance.Cancel();
            //     Coordinator.Instance.ToggleLeadership(request.Name == Params.NODE_NAME, request.Name);
            //     Coordinator.Instance.FreeFollower();
            //     List<MetaNode> _add = request.Add.ToList();
            //     List<string> _flag = request.Flag.ToList();
            //     foreach(MetaNode n0 in _add)
            //     {
            //         if (n0.Name != Params.NODE_NAME && !Ledger.Instance.ContainsKey(n0.Name)
            //             && !Coordinator.Instance.FlaggedCopy.Contains(n0.Name) && !_flag.Contains(n0.Name))
            //         {
            //             Ledger.Instance.AddNode(n0.Name, n0);
            //         }
            //     }
            //     foreach(string n0 in _flag)
            //     {
            //         if (n0 != Params.NODE_NAME && !Coordinator.Instance.FlaggedCopy.Contains(n0))
            //         {
            //             Coordinator.Instance.AddFlag(n0);
            //         }
            //     }
            //     List<MetaNode> _nodes = new List<MetaNode>();
            //     foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
            //     {
            //         if (!_add.Contains(n0.Value) && !_flag.Contains(n0.Key) 
            //             && Coordinator.Instance.FlaggedCopy.Contains(n0.Key)
            //             && n0.Key != request.Name)
            //         {
            //             _nodes.Add(n0.Value);
            //         }
            //     }
            //     Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
            //     Coordinator.Instance.Cancel();

            //     Logger.Log("RequestHandler", "Received AE request - Cluster: [ " + string.Join(", ", Ledger.Instance.ClusterCopy.GetAsToString()) + " ]", Logger.LogLevel.INFO);
            //     if (_nodes.Count > 0) return new AppendEntriesRequest(){
            //         Add = _nodes.ToArray(),
            //         Flag = null
            //     }.EncodeRequest();

            // } catch(Exception e)
            // {
            //     Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR);
            // }
