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
        private List<DriverRequest> queue = new List<DriverRequest>();
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

                        List<DriverRequest> queueCopy = queue.ToList();

                        foreach(DriverRequest exe in queueCopy)
                        {
                            RequestResponse rr = new RequestResponse(){
                                ID = exe.ID,
                                T0 = exe.T0,
                                T1 = Params.Millis,
                                Value = exe.Value
                            };

                            lock (_lock_4)
                            {
                                IRequest response = Client.Run(rr);
                                while (response.TypeOf != RequestType.STATUS) 
                                {
                                    Console.WriteLine("Unable to transmit");
                                    response = Client.Run(rr);
                                }
                                Console.WriteLine("Transmitted");
                                queue.Remove(exe);
                            }
                            
                        }
                    }
                }
            });
        }

        public void ProcessExecute(DriverRequest request)
        {
            // request.
            lock(_lock_1)
            {
                queue.Add(request);
                total++;
                Console.WriteLine("total: " + total.ToString());
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