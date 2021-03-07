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

        public Role _Role {get; set;} = Role.FOLLOWER;
        public long LastHeartbeat;
        public long ElectionTimeout;
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

        public void MarkAsLeader()
        {
            _Role = Role.LEADER;
        }

        public void CastVote(Dictionary<string, QuorumNode> quorum, QuorumNode quorumNode)
        {
            List<string> Votes = new List<string>();
            foreach(string k0 in quorum.Keys)
            {
                NodeClient Client = NodeClient.Connect(quorum[k0].AdvertisedHostName, quorum[k0].Port, quorum[k0].CommonName);
                if (Client != null)
                {
                    Request Response = Client.SendRequestRespondRequest(new Request(){
                        TypeOf = Request.Type.LEADER_ELECTION,
                        Data = new Dictionary<string, DataObject>(){{
                            quorumNode.CommonName, quorumNode.AsDataObject()
                            }},
                        Node = Orchestrator.Instance._Description
                    });
                }
            }
            string majorityVote = Votes.GroupBy( i => i )
                                    .OrderByDescending(group => group.Count())
                                    .ElementAt(0).Key;
        }

        //////////////////////////////////////////////////////////
        ////////////////////////CONSTRUCTOR///////////////////////
        //////////////////////////////////////////////////////////
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
        }
    }
}