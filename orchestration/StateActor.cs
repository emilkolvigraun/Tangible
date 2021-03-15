using System.Collections.Generic;
using System.Linq;
using System;

namespace Node
{
    class StateActor
    {
        public void Process(Coordinator.State state)
        {
            switch(state)
            {
                case Coordinator.State.LEADER:
                    ActAsLeader();
                    break;
                case Coordinator.State.CANDIDATE:
                    ActAsCandidate();
                    break;
                case Coordinator.State.FOLLOWER:
                    ActAsFollower();
                    break;
                default:
                    break;
            }
        }

        private void ActAsFollower()
        {
            foreach (string n0 in Coordinator.Instance.FlaggedCopy)
            {
                Ledger.Instance.RemoveNode(n0);
            }
            Coordinator.Instance.Flagged.Clear();
        }

        private void ActAsCandidate()
        {
            List<string> _votes = new List<string>{Ledger.Instance.Vote};
            foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
            {
                if (n0.Value.Name != Coordinator.Instance.Leader)
                {
                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, RequestType.VT);
                    if (r0.TypeOf == RequestType.VT) _votes.Add(((VotingRequest)r0).Vote);
                } else Coordinator.Instance.AddFlag(n0.Value.Name);
            }
            string majorityVote = _votes.GetMajority();
            Coordinator.Instance.ToggleLeadership(majorityVote == Params.NODE_NAME, majorityVote);
            Logger.Log(this.GetType().Name, "Collected majority vote: " + majorityVote, Logger.LogLevel.DEBUG);
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
        }

        private void ActAsLeader()
        {
            if (Coordinator.Instance.IsCancelled) return;
            try 
            {
                foreach (string n0 in Coordinator.Instance.FlaggedCopy)
                {
                    Ledger.Instance.RemoveNode(n0);
                }
                AppendEntriesRequest ar = new AppendEntriesRequest();
                List<string> tempFlag = new List<string>();
                foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
                {
                    if (Coordinator.Instance.IsCancelled) return;
                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, ar, timeout: true);
                    if(r0.TypeOf == RequestType.AE && ((AppendEntriesRequest)r0).Add != null)
                    {
                        foreach(MetaNode n1 in ((AppendEntriesRequest)r0).Add)
                        {
                            if (n1.Name != Params.NODE_NAME && !Ledger.Instance.ContainsKey(n1.Name) && !Coordinator.Instance.FlaggedCopy.Contains(n1.Name))
                            {
                                Ledger.Instance.AddNode(n1.Name, n1);
                            }
                        }
                    } else if (r0.TypeOf == RequestType.EMPTY)
                    {
                        tempFlag.Add(n0.Key);
                    } 
                    Logger.Log(this.GetType().Name, "Send heartbeat to " + n0.Value.Name + " - " + n0.Value.Host + ":" + n0.Value.Port, Logger.LogLevel.INFO);
                }
                foreach (string n0 in tempFlag)
                {
                    Ledger.Instance.RemoveNode(n0);
                    Logger.Log("ActAsLeader", "[return] Removed " + n0, Logger.LogLevel.WARN);
                }
                Coordinator.Instance.Flagged.Clear();
            } catch (Exception e)
            {
                Logger.Log("ActAsLeader", e.Message, Logger.LogLevel.ERROR);
            }
        }
    }
} 