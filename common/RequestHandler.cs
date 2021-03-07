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
                }
            } catch(Exception e)
            {
                Logger.Log(this.GetType().Name, "ProcessFromType, " + e.Message, Logger.LogLevel.ERROR);
            }
        }

        public void Registration(Request request)
        {      
            try {  
                if (!Orchestrator.Instance.GetLockQuorum().ContainsKey(request.Node.CommonName))
                {
                    NodeClient Client = NodeClient.Connect(request.Node.AdvertisedHostName, request.Node.Port, request.Node.CommonName);
                    if (Client != null)
                    {
                        Logger.Log(this.GetType().Name, "Connected to Client " + request.Node.CommonName, Logger.LogLevel.INFO);
                        
                        byte[] Response = Client.SendRequestRespondBytes(new Request(){
                            TypeOf = Request.Type.CERTIFICATE,
                            Node = Orchestrator.Instance._Description,
                            Data = new Dictionary<string, DataObject>(){{"Cert",new DataObject(){Key = Utils.GetString(Orchestrator.Instance.Certificate)}}}
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
                NetUtils.StoreCertificate(Utils.GetBytes(request.Data["Cert"].Key));
                NetUtils.SendBytes(stream, Orchestrator.Instance.Certificate);
                Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName + " and responded with my own.", Logger.LogLevel.INFO);
                Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Certificate, "+ e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void LeaderElection(Request request, SslStream stream)
        {

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