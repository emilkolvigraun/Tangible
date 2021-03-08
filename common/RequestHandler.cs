using System.Net.Security;
using System.Collections.Generic;
using System.IO;
using System;

namespace Node
{
    class RequestHandler
    {
        public void ProcessFromType(Request request, SslStream stream = null)
        {
            try {
                
                try {
                    if (request.Node.CommonName.Contains(Orchestrator.Instance._Description.CommonName)) return;
                } catch(Exception e) {
                    Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR);
                }
                try {
                    Logger.Log(this.GetType().Name, "Received " + request.TypeOf + " from " + request.Node.CommonName, Logger.LogLevel.INFO);
                } catch(Exception e) {
                    Logger.Log(this.GetType().Name, e.Message, Logger.LogLevel.ERROR);
                }
                
                switch (request.TypeOf)
                {
                    case Request.Type.REGISTRATION: 
                        Registration(request);
                        break;
                    case Request.Type.CERTIFICATE: 
                        Certificate(request, stream);
                        break;
                    case Request.Type.LEADER_ELECTION: 
                        LeaderElection(request, stream);
                        break;
                    case Request.Type.MAJORITY_VOTE: 
                        ValidateMajorityVote(request, stream);
                        break;
                }
            } catch(Exception e)
            {
                Logger.Log(this.GetType().Name, "ProcessFromType, " + e.Message, Logger.LogLevel.ERROR);
            }
        }

        public void Registration(Request request)
        {      
            try {  
                if (!Orchestrator.Instance.GetQuorum().ContainsKey(request.Node.CommonName))
                {
                    NodeClient Client = NodeClient.Connect(request.Node.AdvertisedHostName, request.Node.Port, request.Node.CommonName);
                    if (Client != null)
                    {
                        Logger.Log(this.GetType().Name, "Connected to Client " + request.Node.CommonName, Logger.LogLevel.INFO);
                        
                        byte[] Response = Client.SendRequestRespondBytes(new Request(){
                            TypeOf = Request.Type.CERTIFICATE,
                            Node = Orchestrator.Instance._Description,
                            Data = new DataObject(){V0 = Utils.GetString(Orchestrator.Instance.Certificate)}
                        });
                        Logger.Log(this.GetType().Name, "Responded with CERTIFICATE request to " + request.Node.CommonName, Logger.LogLevel.INFO);
                        NetUtils.StoreCertificate(Response);
                        Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName, Logger.LogLevel.INFO);
                        Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
                    }
                } else {
                    Logger.Log(this.GetType().Name, "Received REGISTRATION, but " + request.Node.CommonName + " was already registered.", Logger.LogLevel.INFO);
                }
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Registration, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void Certificate(Request request, SslStream stream)
        {
            try 
            {
                NetUtils.StoreCertificate(Utils.GetBytes(request.Data.V0));
                NetUtils.SendBytes(stream, Orchestrator.Instance.Certificate);
                Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName + " and responded with my own.", Logger.LogLevel.INFO);
                QuorumNode asQuorum = request.Node.AsQuorum();
                Orchestrator.Instance.RegisterNode(asQuorum);
                Orchestrator.Instance.ContactMemberQuorum(asQuorum);
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Certificate, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void LeaderElection(Request request, SslStream stream)
        {
            QuorumNode myVote = Orchestrator.Instance.MakeVote(request.Data.V2);
            NetUtils.SendRequestResponse( stream, new Request () {
                    Data = new DataObject() {V2 = myVote}
                }
            );
        }
        public void ValidateMajorityVote(Request request, SslStream stream)
        {
            long t0 = Raft.Instance.TsLastLeaderElected;
            string cl = Raft.Instance.CurrentLeader;
            if (t0 == -1 || t0 < request.TimeStamp || cl == null 
                || (request.Data.V0.Contains(cl) && t0.Equals(request.TimeStamp)))
            {
                NetUtils.SendRequestResponse(stream, new Request(){
                    TypeOf = Request.Type.ACCEPTED
                });
                Raft.Instance.TsLastLeaderElected = request.TimeStamp;
                Raft.Instance.CurrentLeader = request.Data.V0;
                Raft.Instance.ResetHeartbeat();
                Raft.Instance.ValidateIfLeader();
            } else { // t0 is greater than the timestamp obtained from the request
                NetUtils.SendRequestResponse(stream, new Request(){
                    TypeOf = Request.Type.NOT_ACCEPTED,
                    TimeStamp = t0,
                    Data = new DataObject(){V0=cl}
                });
                Raft.Instance.ResetHeartbeat();
            }
        }

        public void Heartbeat(Request request, SslStream stream)
        {
            try 
            {
                Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
                NetUtils.SendRequestResponse(stream, new Request(){
                    Node = Orchestrator.Instance._Description,
                });
                Logger.Log(this.GetType().Name, "Responded to heartbeat from "+request.Node.CommonName, Logger.LogLevel.INFO);
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Heartbeat, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }

        private static RequestHandler _instance = null;
        private static readonly object padlock = new object();
        public static RequestHandler Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new RequestHandler();
                    }
                    return _instance;
                }
            }
        }
    }
}