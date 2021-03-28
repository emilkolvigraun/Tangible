using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Node 
{
    class MainLoop 
    {
        long update = Utils.Millis;

        public MainLoop()
        {

        }

        public void Run()
        {
            bool runForever = true;
            while (runForever)
            {
                try 
                {
                    State state = CurrentState.Instance.GetState;

                    switch(state)
                    {
                        case State.LEADER:
                            ActAsLeader();
                            break;
                        case State.CANDIDATE:
                            ActAsCandidate();
                            break;     
                        case State.FOLLOWER:
                            ActAsFollower();
                            break;
                        case State.SLEEPING:
                            ActAsSleeper();
                            break;
                    }

                    if(update+1000<Utils.Millis){
                        Logger.LogState();
                        update = Utils.Millis;
                    }

                } catch(Exception e)
                {
                    Logger.Log("MainLoop", "Uncaugt exception: " + e.Message, Logger.LogLevel.ERROR);
                }
            }

        }

        public void ActAsCandidate()
        {
            try 
            {
                // Start current election term
                CurrentState.Instance.StartElectionTerm();

                // Retrieving a copy of all the nodes within the cluster
                List<string> clients = Cluster.Instance.ClientIds;

                // Init a list of tasks to reach out to all other nodes in parallel
                List<Task> tasks = new List<Task>();

                // Create vote request
                VotingRequest vote = new VotingRequest(){
                    Vote = Params.NODE_NAME
                };

                foreach(string nodeId in clients)
                {
                    try 
                    {
                        if (!CurrentState.Instance.IsElectionTerm) break;

                        tasks.Add(new Task(() => {
                            IRequest r0 = Cluster.Instance.GetClient(nodeId).Run(vote);
                            if (r0.TypeOf != RequestType.EMPTY 
                                && r0.TypeOf == RequestType.VOTE 
                                && ((VotingRequest)r0).Vote != vote.Vote)
                            {
                                CurrentState.Instance.StopElectionTerm();
                            }

                            Logger.Log("ActAsCandidate", "Send vote to [node:" + nodeId+"]", Logger.LogLevel.INFO);
                        }));
                    } catch(Exception e)
                    {
                        Logger.Log("ActAsCandidate", "[1] " + e.Message, Logger.LogLevel.ERROR);
                    }
                }

                if (CurrentState.Instance.IsElectionTerm)
                {
                    Task[] _tasks = tasks.ToArray();
                    Parallel.ForEach<Task>(_tasks, (t) => { t.Start(); });
                    Task.WaitAll(_tasks, Params.CONNECT_TIMEOUT*3);
                }
                
                if (CurrentState.Instance.IsElectionTerm) CurrentState.Instance.SetIsLeader(true);
                else 
                {
                    CurrentState.Instance.SetIsLeader(false);
                    // Reset heartbeat
                    CurrentState.Instance.ResetHeartbeat();
                }
                
            } catch(Exception e)
            {
                Logger.Log("ActAsCandidate", "[0] " + e.Message, Logger.LogLevel.ERROR);
            }
        }
        public void ActAsLeader()
        {
            long t0 = Utils.Millis;

            if (RequestQueue.Instance.CountUnfinished > 0)
                ActAsSleeper();

            Cluster.Instance.IncrementHeartbeats();

            foreach(string node in Cluster.Instance.ClientIds)
            {
                if (Cluster.Instance.IsTimedOut(node))
                {
                    Ledger.Instance.RescheduleRequests(node);
                    Ledger.Instance.RemoveClient(node);
                    Cluster.Instance.RemoveClient(node);
                }
            }

            ActionRequest request = PriorityQueue.Instance.Dequeue();
            if (request!=null)
            {
                Ledger.Instance.ScheduleRequest(request);
            }
            
            // Init a list of tasks to reach out to all other nodes in parallel
            List<Task> tasks = new List<Task>();
            foreach(string node in Cluster.Instance.ClientIds)
            {
                tasks.Add(new Task(()=>{
                    (Dictionary<string, List<string>> LogDetach, Dictionary<string, List<Request>> LogAppend, List<Request> Enqueue, List<string> NodeDetach, List<Node> NodeAttach, List<string> Dequeue, HashSet<string> PriorityDetach, List<ActionRequest> PriorityAttach) update = Ledger.Instance.GetClientUpdates(node);
                    AppendEntryRequest appendEntry = new AppendEntryRequest(){
                        LogDetach = update.LogDetach,
                        LogAppend = update.LogAppend,
                        Enqueue = update.Enqueue,
                        Dequeue = update.Dequeue,
                        NodeDetach = update.NodeDetach,
                        NodeAttach = update.NodeAttach,
                        PriorityDetach = update.PriorityDetach,
                        PriorityAttach = update.PriorityAttach
                    };
                    IRequest Response = Cluster.Instance.GetClient(node).Run(appendEntry);
                    if (Response.TypeOf == RequestType.APPEND_RES && ((AppendEntryResponse)Response).Status)
                    {
                        AppendEntryResponse aes = (AppendEntryResponse)Response;
                        Cluster.Instance.ResetHeartbeat(node);
                        Ledger.Instance.UpdateClientState(node, update.LogDetach, update.LogAppend, update.Enqueue, update.NodeDetach, update.NodeAttach, update.Dequeue, aes.Completed, update.PriorityDetach, update.PriorityAttach);
                    }
                }));
            }

            Task[] _tasks = tasks.ToArray();
            Parallel.ForEach<Task>(_tasks, (t) => { t.Start(); });
            Task.WaitAll(_tasks, Params.CONNECT_TIMEOUT*3);
            long delta = Utils.Millis - t0;
            if (delta < 0) delta=0;
            if (delta > 10) delta=10;
            Utils.Wait(10-((int) delta));
        }

        public void ActAsSleeper()
        {
            try 
            {
                ActionRequest request = PriorityQueue.Instance.Dequeue();
                if (request!=null)
                {
                    Ledger.Instance.ScheduleRequest(request);
                }
                SleeperRequestQueue();
            } catch(Exception e)
            {
                Logger.Log("ActAsSleeper", e.Message, Logger.LogLevel.ERROR);
            }
        }

        private void SleeperRequestQueue(int index = 0)
        {
            Request request = RequestQueue.Instance.Peek(index);

            if (request != null)
            {
                bool status = Containers.Instance.ExecuteRequest(request);
                if (!status) SleeperRequestQueue(index+1);
                else 
                {
                    RequestQueue.Instance.Dequeue();
                }
                
                // RequestQueue.Instance.DetachRequest(request.ID);
            }
        }

        public void ActAsFollower(int index = 0)
        {
            Request request = RequestQueue.Instance.Peek(index);

            if (request != null)
            {
                bool status = Containers.Instance.ExecuteRequest(request);
                if (!status) ActAsFollower(index+1);
                else RequestQueue.Instance.Dequeue();

                RequestQueue.Instance.CompleteRequest(request.ID);
            }
        }
    }
}