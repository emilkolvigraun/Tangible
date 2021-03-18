using System;

namespace Node 
{
    class PriorityQueue_Test
    {
        public static void Run()
        {
            // DEBUGGING 
            Environment.SetEnvironmentVariable("KAFKA_BROKERS", "192.168.1.237:9092");
            Environment.SetEnvironmentVariable("CLUSTER_ID", "Tangible#1");
            Environment.SetEnvironmentVariable("REQUEST_TOPIC", "Tangible.request.1");
            Environment.SetEnvironmentVariable("BROADCAST_TOPIC", "Tangible.broadcast.1");
            Environment.SetEnvironmentVariable("ADVERTISED_HOST_NAME", "192.168.1.237");
            Environment.SetEnvironmentVariable("PORT_NUMBER", "5001");
            Environment.SetEnvironmentVariable("WAIT_TIME_MS", "1000");
            Environment.SetEnvironmentVariable("NODE_NAME", "node1");
            
            // load environment variables
            Params.LoadConfig();
            PriorityQueue queue = new PriorityQueue();
            for(int i = 0; i < 3; i++)
                queue.EnqueueJob(new Job(){
                    TypeOf = Job.Type.OP,
                    StatusOf = Job.Status.NS,
                    Request = new ActionRequest(){
                        TypeOf = RequestType.SUBSCRIBE,
                        Priority = 2
                    }
                });
            queue.DequeueJob();
            for(int i = 0; i < 4; i++)
                queue.EnqueueJob(new Job(){
                    TypeOf = Job.Type.OP,
                    StatusOf = Job.Status.NS,
                    Request = new ActionRequest(){
                        TypeOf = RequestType.SUBSCRIBE,
                        Priority = 1
                    }
                });
            queue.DequeueJob();
            queue.DequeueJob();
            for(int i = 0; i < 4; i++)
                queue.EnqueueJob(new Job(){
                    TypeOf = Job.Type.OP,
                    StatusOf = Job.Status.NS,
                    Request = new ActionRequest(){
                        TypeOf = RequestType.SUBSCRIBE,
                        Priority = 0
                    }
                });
            queue.DequeueJob();
            queue.DequeueJob();
            for(int i = 0; i < 3; i++)
                queue.EnqueueJob(new Job(){
                    TypeOf = Job.Type.OP,
                    StatusOf = Job.Status.NS,
                    Request = new ActionRequest(){
                        TypeOf = RequestType.SUBSCRIBE,
                        Priority = 3
                    }
                });
            queue.DequeueJob();
            queue.DequeueJob();
            // 3, 1, 1, 0, 0, 0, 0
            foreach (Job job in queue.Queue)
            {
                Console.Write(job.Request.Priority + ",");
            }
            Console.WriteLine();
            Environment.Exit(0);
        }
    }
}