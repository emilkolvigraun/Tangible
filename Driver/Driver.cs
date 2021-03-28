using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Driver 
{
    class Driver 
    {
        private readonly object _lock_1 = new object();
        private readonly object _lock_2 = new object();
        private readonly object _lock_3 = new object();
        private readonly object _lock_4 = new object();

        private int total = 0;

        private NodeClient Client = new NodeClient();
        private List<Execute> queue = new List<Execute>();
        private Dictionary<string, List<RequestResponse>> memory = new Dictionary<string, List<RequestResponse>>();
        private Dictionary<string, RunAsRequest> report = new Dictionary<string, RunAsRequest>();

        public void Run()
        {
            Console.WriteLine("Running driver loop...");
            Task.Run(() => {
                while(true)
                {
                    lock(_lock_1)
                    {

                        List<Execute> queueCopy = queue.ToList();

                        foreach(Execute exe in queueCopy)
                        {
                            RequestResponse rr = new RequestResponse(){
                                JobId = exe.JobID,
                                Status = true,
                                Value = exe.Value==null?"value":exe.Value,
                                PointID = exe.PointID,
                                TimeStamp = Params.Millis
                            };

                            if (exe.TypeOfAction == ActionType.SUBSCRIBE)
                            {

                                lock (_lock_2)
                                {

                                    if (report.ContainsKey(exe.JobID))
                                    {
                                        exe.JobType = JobType.OP;
                                    }

                                    if (exe.JobType == JobType.OP)
                                    {
                                        lock(_lock_3)
                                        {
                                            bool status = true;
                                            if (memory.ContainsKey(exe.JobID))
                                            {
                                                List<RequestResponse> rrl = memory[exe.JobID].ToList(); 
                                                for (int i = 0; i < rrl.Count; i++)
                                                {
                                                    IRequest r0 = Client.WriteRequest(Params.NODE_HOST, Params.NODE_PORT, Params.NODE_NAME, rrl[i]);
                                                    if (r0.TypeOf == RequestType.ST) 
                                                    {
                                                        memory[exe.JobID].RemoveAt(i);
                                                    } else 
                                                    {   
                                                        status = false;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (status)
                                            {
                                                IRequest r1 = Client.WriteRequest(Params.NODE_HOST, Params.NODE_PORT, Params.NODE_NAME, rr);
                                                // if (r1.TypeOf == RequestType.ST) 
                                                // {
                                                //     queue.Remove(exe);
                                                // }
                                            }
                                        }

                                        
                                    } else 
                                    {
                                        lock(_lock_3)
                                        {
                                            if (!memory.ContainsKey(exe.JobID))
                                            {
                                                memory.Add(exe.JobID, new List<RequestResponse>());
                                            }

                                            memory[exe.JobID].Add(rr);

                                            if (memory[exe.JobID].Count > 200)
                                                memory[exe.JobID].RemoveAt(0);
                                        }
                                    }
                                }
                            } else {
                                lock (_lock_4)
                                {
                                    IRequest response = Client.WriteRequest(Params.NODE_HOST, Params.NODE_PORT, Params.NODE_NAME, rr);
                                    while (response.TypeOf != RequestType.ST) 
                                    {
                                        response = Client.WriteRequest(Params.NODE_HOST, Params.NODE_PORT, Params.NODE_NAME, rr);
                                    }
                                    queue.Remove(exe);
                                }
                            }
                        }
                    }
                }
            });
        }

        public void ProcessExecute(Execute request)
        {
            // request.
            lock(_lock_1)
            {
                queue.Add(request);
                total++;
                Console.WriteLine(request.EncodeRequestStr() + ", total Execute: " + total.ToString());
            }
        }

        public void ProcessRunAs(RunAsRequest request)
        {
            // request.

            lock(_lock_2)
            {
                report.Add(request.JobID, request);
                Console.WriteLine(request.EncodeRequestStr() + ", total RunAs: " + report.Count);
            }
        }
    }
}