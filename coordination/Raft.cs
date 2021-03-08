using System.Collections.Generic;
using System.Linq;

namespace Node
{
    class Raft
    {
        public enum Role 
        {
            LEADER,
            FOLLOWER,
            CANDIDATE
        }

        public Role _Role {get; set;} = Role.CANDIDATE;
        public long LastHeartbeat;
        public long ElectionTimeout;
        public long TsLastLeaderElected;
        public string CurrentLeader;

        public Role GetState()
        {
            if (LastHeartbeat - Utils.Millis > ElectionTimeout) _Role = Role.CANDIDATE;
            else if (_Role != Role.LEADER) _Role = Role.FOLLOWER;
            return _Role;
        }

        public void UpdateLastHeartbeat()
        {
            LastHeartbeat = Utils.Millis;
        }

        public void ValidateIfLeader()
        {
            lock (roleLock) 
            {
                if (CurrentLeader.Contains(Orchestrator.Instance._Description.CommonName))
                    _Role = Role.LEADER;
            }
        }

        public void CastVote(Dictionary<string, QuorumNode> quorum, QuorumNode quorumNode)
        {
            lock(roleLock)
            {
                List<string> Votes = new List<string>(){quorumNode.CommonName};
                foreach(string k0 in quorum.Keys)
                {
                    NodeClient Client = NodeClient.Connect(quorum[k0].AdvertisedHostName, quorum[k0].Port, quorum[k0].CommonName);
                    if (Client != null)
                    {
                        Request Response = Client.SendRequestRespondRequest(new Request(){
                            TypeOf = Request.Type.LEADER_ELECTION,
                            Data = new DataObject(){
                                    V2 = quorumNode
                                },
                            Node = Orchestrator.Instance._Description
                        });
                        Votes.Add(Response.Data.V2.CommonName);
                    }
                }
                string majorityVote = Votes.GroupBy( i => i ).OrderByDescending(group => group.Count()).ElementAt(0).Key;
                CurrentLeader = majorityVote;
                TsLastLeaderElected = Utils.Millis;
                VerifyMajorityVote(quorum);
                ValidateIfLeader();
            }
        }

        private void VerifyMajorityVote(Dictionary<string, QuorumNode> quorum)
        {
            foreach(string k0 in quorum.Keys)
            {
                NodeClient Client = NodeClient.Connect(quorum[k0].AdvertisedHostName, quorum[k0].Port, quorum[k0].CommonName);
                if (Client != null)
                {
                    Request Response = Client.SendRequestRespondRequest(new Request(){
                        TypeOf = Request.Type.MAJORITY_VOTE,
                        TimeStamp = TsLastLeaderElected,
                        Data = new DataObject(){V0 = CurrentLeader}
                    });

                    if (Response.TypeOf == Request.Type.NOT_ACCEPTED)
                    {
                        CurrentLeader = Response.Data.V0;
                        TsLastLeaderElected = Response.TimeStamp;
                        VerifyMajorityVote(quorum);
                        break;
                    }
                }
            }
        }

        public void ResetHeartbeat()
        {
            lock (hbLock)
            {
                LastHeartbeat = Utils.Millis;
            }
        }

        //////////////////////////////////////////////////////////
        ////////////////////////CONSTRUCTOR///////////////////////
        //////////////////////////////////////////////////////////
        public static readonly object hbLock = new object();
        public static readonly object roleLock = new object();
        public static readonly object padlock = new object();
        public static Raft _instance = null;
        public static Raft Instance
        {
            get {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new Raft();
                    }
                    return _instance;
                }
            }
        }

        Raft()
        {
            ElectionTimeout = OrchestrationVariables.HEARTBEAT_MS;
            LastHeartbeat = Utils.Millis;
            TsLastLeaderElected = -1;
        }
    }
}