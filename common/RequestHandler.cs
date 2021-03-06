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
                if (request.Node.CommonName.Contains(Orchestrator.Instance._Description.CommonName)) return;
                
                Logger.Log(this.GetType().Name, "Received " + request.TypeOf + " from " + request.Node.CommonName);
                switch (request.TypeOf)
                {
                    case Request.Type.REGISTRATION: 
                        Registration(request);
                        break;
                    case Request.Type.CERTIFICATE: 
                        Certificate(request, stream);
                        break;
                    case Request.Type.HEARTBEAT: 
                        Heartbeat(request, stream);
                        break;
                }
            } catch(Exception e)
            {
                Logger.Log(this.GetType().Name, "ProcessFromType, " + e.Message);
            }
        }

        public void Registration(Request request)
        {      
            try {  
                NodeClient Client = NodeClient.Connect(request.Node.AdvertisedHostName, request.Node.Port, request.Node.CommonName);
                if (Client != null)
                {
                    byte[] Response = Client.SendRequestRespondBytes(new Request(){
                        TypeOf = Request.Type.CERTIFICATE,
                        Node = Orchestrator.Instance._Description,
                        Data = new Dictionary<string, string>(){{"Cert",Utils.GetString(Orchestrator.Instance.Certificate)}}
                    });
                    Logger.Log(this.GetType().Name, "Responded with CERTIFICATE request to " + request.Node.CommonName);
                    NetUtils.StoreCertificate(Response);
                    Logger.Log(this.GetType().Name, "Stored certificate from " + request.Node.CommonName);
                    Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
                    Logger.Log(this.GetType().Name, "Registered " + request.Node.CommonName + " to Quorum of size: " + Orchestrator.Instance.GetLockQuorum().Count.ToString());
                }
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Registration, "+ e.Message);
            }
        }

        public void Certificate(Request request, SslStream stream)
        {
            try 
            {
                NetUtils.StoreCertificate(Utils.GetBytes(request.Data["Cert"]));
                NetUtils.SendBytes(stream, Orchestrator.Instance.Certificate);
                Logger.Log(this.GetType().Name, "Responded with certificate");
                Orchestrator.Instance.RegisterNode(request.Node.AsQuorum());
                    Logger.Log(this.GetType().Name, "Registered " + request.Node.CommonName + " to Quorum of size: " + Orchestrator.Instance.GetLockQuorum().Count.ToString());
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Certificate, "+ e.Message);
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
                Logger.Log(this.GetType().Name, "Responded to heartbeat from "+request.Node.CommonName);
            } catch (Exception e)
            {
                Logger.Log(this.GetType().Name, "Heartbeat, "+ e.Message);
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