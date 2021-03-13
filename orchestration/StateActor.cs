using System.Collections.Generic;
using System.Linq;

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
                Coordinator.Instance.RemoveFlag(n0);
            }
        }

        private void ActAsCandidate()
        {
            if (Coordinator.Instance.IsCancelled) return;
            List<string> _votes = new List<string>{Ledger.Instance.Vote};
            foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
            {
                if (Coordinator.Instance.IsCancelled) break;
                if (n0.Value.Name != Coordinator.Instance.Leader)
                {
                    IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, RequestType.VT);
                    if (r0.TypeOf == RequestType.VT) _votes.Add(((VotingRequest)r0).Vote);
                } else Coordinator.Instance.AddFlag(n0.Value.Name);
            }
            if (!Coordinator.Instance.IsCancelled)
            {
                string majorityVote = _votes.GetMajority();
                Coordinator.Instance.ToggleLeadership(majorityVote == Params.NODE_NAME, majorityVote);
                Logger.Log(this.GetType().Name, "Collected majority vote: " + majorityVote, Logger.LogLevel.DEBUG);
            }
            Coordinator.Instance.SetPenalty(Params.HEARTBEAT_MS);
        }

        private void ActAsLeader()
        {
            foreach (string n0 in Coordinator.Instance.FlaggedCopy)
            {
                Ledger.Instance.RemoveNode(n0);
                Coordinator.Instance.RemoveFlag(n0);
            }
            AppendEntriesRequest ar = new AppendEntriesRequest();
            foreach(KeyValuePair<string, MetaNode> n0 in Ledger.Instance.ClusterCopy)
            {
                IRequest r0 = NodeClient.RunClient(n0.Value.Host, n0.Value.Port, n0.Value.Name, ar);

                Logger.Log(this.GetType().Name, "Send heartbeat to " + n0.Value.Name + " - " + n0.Value.Host + ":" + n0.Value.Port, Logger.LogLevel.INFO);
            }
        }
    }
} 