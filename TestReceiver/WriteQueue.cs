using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Server 
{
    class WriteQueue
    {
        private static readonly object _lock = new object();
        private static WriteQueue _instance = null;

        public static WriteQueue Instance 
        {
            get 
            {
                if(_instance==null) _instance = new WriteQueue();
                return _instance;
            }
        }

        private Queue<Response> _queue = new Queue<Response>();

        public void Enqueue(Response r0)
        {
            lock(_lock)
            {
                _queue.Enqueue(r0);
            }
        }

        public Response Dequeue()
        {
            lock(_lock)
            {
                if (_queue.Count < 1) return null;
                return _queue.Dequeue();
            }
        }

        WriteQueue()
        {
            string path = @"received.txt";
            string line = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", 
                            "T0", "T1", "T2", "T3", "Value", "Jobs", "Heartbeat", "Cluster", "Name");
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(line);
                }	
            }
        }


        public void Run ()
        {
            while (true)
            {
                Response response = null;
                lock(_lock)
                {
                    response = Dequeue();
                }

                if (response!=null)
                {
                    string line = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", 
                        response.T0.ToString(), response.T1.ToString(), response.T2.ToString(), response.T3.ToString(),
                        response.Value.ToString(), response.Jobs.ToString(), response.Heartbeat.ToString(), response.Cluster.ToString(), response.Name.ToString());
                    
                    string path = @"received.txt";
                    // This text is added only once to the file.
                    if (!File.Exists(path))
                    {
                        // Create a file to write to.
                        using (StreamWriter sw = File.CreateText(path))
                        {
                            sw.WriteLine(line);
                        }	
                    } else 
                    {
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine(line);
                        }	
                    }
                
                    Console.WriteLine(response.EncodeRequestStr());
                }
            }
        }
    }
}