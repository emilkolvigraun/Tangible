using System;
using System.Collections.Generic;
using System.Linq;

namespace Node
{
    class Program
    {

        static void LoadEnvVariables()
        {
            EsbVariables.Load();
            NetVariables.Load();
            OrchestrationVariables.Load();
        }

        static void Main(string[] args)
        {
            // Environment.SetEnvironmentVariable("KAFKA_BROKERS", "192.168.1.237:9092");
            // Environment.SetEnvironmentVariable("BROADCAST_TOPIC", "Tangible.broadcast.1");
            // Environment.SetEnvironmentVariable("ADVERTISED_HOST_NAME", "192.168.1.237");
            // Environment.SetEnvironmentVariable("PORT_NUMBER", "8000");
            // Environment.SetEnvironmentVariable("INTERFACE", "0.0.0.0");
            // Environment.SetEnvironmentVariable("CERT_EXPIRE_DAYS", "365");
            // Environment.SetEnvironmentVariable("CLUSTER_ID", "Tangible#1");
            // Environment.SetEnvironmentVariable("REQUEST_TOPIC", "Tangible.request.1");
            // Environment.SetEnvironmentVariable("HEARTBEAT_S", "3");
            LoadEnvVariables();

            Logger.Log("MAIN", "Preparing ingredients..");
            long t0 = Utils.Millis;
            while (true)
            {
                if (Utils.Millis - t0 >= EsbVariables.WAIT_TIME_S) break;
            }

            NetUtils.MakeCertificate();
            AsyncTLSServer.Instance.AsyncStart();

            Orchestrator.Instance._Description = new Description() {
                CommonName = NetUtils.LoadCommonName(),
                AdvertisedHostName = NetVariables.ADVERTISED_HOST_NAME,
                Port = NetVariables.PORT_NUMBER,
                Workload = Orchestrator.Instance.CalculateWorkloadBurden(),
                Quorum = new Dictionary<string, QuorumNode>(),
                Tasks = new List<Task>(),
            };

            Console.CancelKeyPress += delegate {
                AsyncTLSServer.Instance.Stop();
                KafkaConsumer.Instance.Dispose();
                Orchestrator.Instance.Dispose();
            };


            Logger.Log("MAIN", "Started with ID: "+Orchestrator.Instance._Description.CommonName);
            Logger.Log("MAIN", "- address: "+Orchestrator.Instance._Description.AdvertisedHostName+":"+Orchestrator.Instance._Description.Port);
            Logger.Log("MAIN", "- Kafka brokers: "+EsbVariables.KAFKA_BROKERS);
            Logger.Log("MAIN", "- Group: "+EsbVariables.CLUSTER_ID);
            Logger.Log("MAIN", "- boradcast topic: "+EsbVariables.BROADCAST_TOPIC);
            Logger.Log("MAIN", "- request topic: "+EsbVariables.REQUEST_TOPIC);
            Logger.Log("MAIN", "- heartbeat ms: "+OrchestrationVariables.HEARTBEAT_S);

            Orchestrator.Instance.Init();
            Orchestrator.Instance.Run();
            // Console.ReadLine();
        }
    }
}
