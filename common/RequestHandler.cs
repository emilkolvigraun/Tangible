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
                    if (request.Node!=null && 
                        request.Node.CommonName.Contains(Orchestrator.Instance._Description.CommonName)) return;
                } catch(Exception e) {
                    Logger.Log(this.GetType().Name, "ProcessFromType," + e.Message, Logger.LogLevel.ERROR);
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
                if (request.Node.AdvertisedHostName.Contains(Orchestrator.Instance._Description.AdvertisedHostName) && request.Node.Port.Equals(Orchestrator.Instance._Description.Port))
                {
                    throw new DuplicateWaitObjectException("Received a registration request from a duplication server");
                }
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
                        if (Response != null)
                        {
                            Logger.Log(this.GetType().Name, "Sent CERTIFICATE request to " + request.Node.CommonName, Logger.LogLevel.INFO);
                            NetUtils.StoreCertificate(Response);
                            Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName, Logger.LogLevel.INFO);
                            Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
                        } else 
                        {
                            Logger.Log(this.GetType().Name, "Response was null.", Logger.LogLevel.ERROR);
                        }
                    }
                } else {
                    Logger.Log(this.GetType().Name, "Received REGISTRATION, but " + request.Node.CommonName + " was already registered.", Logger.LogLevel.INFO);
                }
            } catch (DuplicateWaitObjectException e)
            {
                Logger.Log(this.GetType().Name, "Registration, "+ e.Message, Logger.LogLevel.FATAL);
                Environment.Exit(0);
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Registration, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void Certificate(Request request, SslStream stream)
        {
            try 
            {
                // Raft.Instance.SetPenalty(1000, Utils.Millis);
                NetUtils.StoreCertificate(Utils.GetBytes(request.Data.V0));
                NetUtils.SendBytes(stream, Orchestrator.Instance.Certificate);
                QuorumNode asQuorum = request.Node.AsQuorum();
                Orchestrator.Instance.RegisterNode(asQuorum);
                Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName + " and responded with my own", Logger.LogLevel.INFO);
                Orchestrator.Instance.ContactMemberQuorum(asQuorum);

                if (Raft.Instance.CurrentLeader == null)
                {
                    Raft.Instance.CastVote(
                        Orchestrator.Instance.CopyQuorum(), 
                        Orchestrator.Instance.MakeVote()
                    );  
                }
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Certificate, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void LeaderElection(Request request, SslStream stream)
        {
            try {
                // Raft.Instance.SetPenalty(1000, Utils.Millis);
                QuorumNode myVote = Orchestrator.Instance.MakeVote();
                NetUtils.SendRequestResponse( stream, new Request () {
                        Data = new DataObject() {V2 = myVote}
                    }
                );
            }
            catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "LeaderElection," + e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void ValidateMajorityVote(Request request, SslStream stream)
        {
            try 
            {
                // Raft.Instance.SetPenalty(1000, Utils.Millis);
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
                    bool ifLeader = Raft.Instance.ValidateIfLeader();
                    if (ifLeader) 
                    {
                        if (!KafkaConsumer.Instance.subscribing) KafkaConsumer.Instance.Subscribe();
                        Logger.Log(this.GetType().Name, "I am the leader " + request.Data.V0, Logger.LogLevel.INFO);
                    }
                    else 
                    {
                        KafkaConsumer.Instance.Dispose();
                        Logger.Log(this.GetType().Name, "Unsubscribed to topics.", Logger.LogLevel.INFO);
                        Logger.Log(this.GetType().Name, "Set leader to " + request.Data.V0, Logger.LogLevel.INFO);
                    }
                } else { // t0 is greater than the timestamp obtained from the request
                    NetUtils.SendRequestResponse(stream, new Request(){
                        TypeOf = Request.Type.NOT_ACCEPTED,
                        TimeStamp = t0,
                        Data = new DataObject(){V0=cl}
                    });
                    Raft.Instance.ResetHeartbeat();
                }
            }
            catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "ValidateMajorityVote, " + e.Message, Logger.LogLevel.ERROR);
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