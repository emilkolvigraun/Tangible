using System;
using System.Threading;
using System.Threading.Tasks;

namespace Node
{
    class Program
    {
        static void DisplayStartInformation()
        {
            Utils.Wait();
            Logger.Log("Main", "Starting with parameters:", Logger.LogLevel.INFO);
            Logger.Log("Main", "- ADDRESS: " + Params.ADVERTISED_HOST_NAME + ":" + Params.PORT_NUMBER, Logger.LogLevel.INFO);
            Logger.Log("Main", "- NODE_NAME: " + Params.NODE_NAME, Logger.LogLevel.INFO);
            Logger.Log("Main", "- KAFKA_BROKERS: " + Params.KAFKA_BROKERS, Logger.LogLevel.INFO);
            Logger.Log("Main", "- CLUSTER_ID: " + Params.CLUSTER_ID, Logger.LogLevel.INFO);
            Logger.Log("Main", "- BROADCAST_TOPIC: " + Params.BROADCAST_TOPIC, Logger.LogLevel.INFO);
            Logger.Log("Main", "- REQUEST_TOPIC: " + Params.REQUEST_TOPIC, Logger.LogLevel.INFO);
            Logger.Log("Main", "- UNIQUE_ID: " + Params.UNIQUE_KEY, Logger.LogLevel.INFO);
            Logger.Log("Main", "- ELECTION_TIMEOUT: " + Params.HEARTBEAT_MS +"ms", Logger.LogLevel.INFO);
        }

        static void AsDebug()
        {
            Environment.SetEnvironmentVariable("KAFKA_BROKERS", "192.168.1.237:9092,192.168.1.237:9093");
            Environment.SetEnvironmentVariable("CLUSTER_ID", "Tangible#1");
            Environment.SetEnvironmentVariable("REQUEST_TOPIC", "Tangible.request.1");
            Environment.SetEnvironmentVariable("BROADCAST_TOPIC", "Tangible.broadcast.1");
            Environment.SetEnvironmentVariable("ADVERTISED_HOST_NAME", "192.168.1.237");
            Environment.SetEnvironmentVariable("PORT_NUMBER", "5001");
            Environment.SetEnvironmentVariable("WAIT_TIME_MS", "1000");
            Environment.SetEnvironmentVariable("NODE_NAME", "node1");
            Environment.SetEnvironmentVariable("DRIVER_RANGE", "6000->6100");
            Environment.SetEnvironmentVariable("DOCKER_ADVERTISED_HOST_NAME", "npipe://./pipe/docker_engine");
        }

        static Thread Init()
        {
            Params.LoadConfig();
            Task.Run(async () => {
                await DockerAPI.Instance.RemoveStoppedContainers();
            }).Wait();
            DisplayStartInformation();
            MainLoop loop = new MainLoop();

            Thread loopThread = new Thread(() => {
                loop.Run();
            });

            // Task.Run(() => {
            //     foreach(string img in HardwareAbstraction.Images)
            //     {
            //         Containers.Instance.CreateContainer(img);
            //         while (true) {if(!Containers.Instance.IsBusy)break;}
            //     }
            // }).Wait();

            return loopThread;
        }

        static void RunTests()
        {
            // Request_test.Run();
            // Environment.Exit(0);
        }

        static void Broadcast()
        {
            Task.Run(() => {
                Utils.Wait(1000);
                long t0 = Utils.Millis;
                bool status = false;
                while (t0+2000 >= Utils.Millis)
                {
                    (bool Status, string[] failures) Result = Producer.Instance.Send(new BroadcastRequest(){
                        _Node = new Node()
                        {
                            Name = Params.NODE_NAME,
                            Key = Params.UNIQUE_KEY,
                            Host = Params.ADVERTISED_HOST_NAME,
                            Port = Params.PORT_NUMBER
                        }
                    }.EncodeRequestStr(), new string[] {Params.BROADCAST_TOPIC});
                    status = Result.Status;
                    if (status) 
                    {
                        Logger.Log("Main", "Broadcasted to " + Params.BROADCAST_TOPIC, Logger.LogLevel.INFO);
                        break;
                    } 
                    Logger.Log("Main", "Failed to broadcast, retrying in 1 second...", Logger.LogLevel.WARN);
                    Utils.Wait(1000);
                }
                if (!status) Logger.Log("Broadcast", "Failed to broadcast. Exiting...", Logger.LogLevel.FATAL);
            
                Utils.Wait(500);
                if (Cluster.Instance.Count == 0 || CurrentState.Instance.IsLeader) Consumer.Instance.Start(new string[]{Params.BROADCAST_TOPIC, Params.REQUEST_TOPIC});
            });
        }

        static void Main(string[] args)
        {
            // DEBUGGING 
            AsDebug();

            Thread loop = Init();
            
            // DEBUGGING
            RunTests();

            
            loop.Start();
            Broadcast();
            NodeServer.RunServer();
        }
    }
}
