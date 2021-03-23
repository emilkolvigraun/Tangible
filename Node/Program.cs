﻿using System;
using System.Threading;

namespace Node
{
    class Program
    {

        static void DisplayStartInformation()
        {
            Logger.Log("Main", "Starting with parameters:", Logger.LogLevel.INFO);
            Logger.Log("Main", "- ADDRESS: " + Params.ADVERTISED_HOST_NAME + ":" + Params.PORT_NUMBER, Logger.LogLevel.INFO);
            Logger.Log("Main", "- NODE_NAME: " + Params.NODE_NAME, Logger.LogLevel.INFO);
            Logger.Log("Main", "- KAFKA_BROKERS: " + Params.KAFKA_BROKERS, Logger.LogLevel.INFO);
            Logger.Log("Main", "- CLUSTER_ID: " + Params.CLUSTER_ID, Logger.LogLevel.INFO);
            Logger.Log("Main", "- BROADCAST_TOPIC: " + Params.BROADCAST_TOPIC, Logger.LogLevel.INFO);
            Logger.Log("Main", "- REQUEST_TOPIC: " + Params.REQUEST_TOPIC, Logger.LogLevel.INFO);
            Logger.Log("Main", "- UNIQUE_ID: " + Params.UNIQUE_KEY, Logger.LogLevel.INFO);
            Logger.Log("Main", "- ELECTION_TIMEOUT: " + Params.HEARTBEAT_MS +"ms", Logger.LogLevel.INFO);
            // Logger.Log("Main", "% Processor Time: " + Params.USAGE, Logger.LogLevel.INFO);
        }

        static void Main(string[] args)
        {
            // DEBUGGING 
            Environment.SetEnvironmentVariable("KAFKA_BROKERS", "192.168.1.237:9092");
            Environment.SetEnvironmentVariable("CLUSTER_ID", "Tangible#1");
            Environment.SetEnvironmentVariable("REQUEST_TOPIC", "Tangible.request.1");
            Environment.SetEnvironmentVariable("BROADCAST_TOPIC", "Tangible.broadcast.1");
            Environment.SetEnvironmentVariable("ADVERTISED_HOST_NAME", "192.168.1.237");
            Environment.SetEnvironmentVariable("PORT_NUMBER", "5002");
            Environment.SetEnvironmentVariable("WAIT_TIME_MS", "1000");
            Environment.SetEnvironmentVariable("NODE_NAME", "node2");
            // Environment.SetEnvironmentVariable("DOCKER_HOST_NAME", "tcp://192.168.1.237:4243");
            Environment.SetEnvironmentVariable("DOCKER_HOST_NAME", "npipe://./pipe/docker_engine");
            
            // load environment variables
            Params.LoadConfig();

            // SerializeDeserialize_test.Run();

            Utils.Wait();

            DisplayStartInformation();

            // initializing server thread
            Thread serverThread = new Thread(() => {
                NodeServer.RunServer();
            });

            // running internal comunication 
            serverThread.Start();
            
            Utils.Wait(1);

            // sending the broadcast
            while (true)
            {
                (bool Status, string[] failures) Result = Producer.Instance.Send(new BroadcastRequest().EncodeRequestStr(), new string[] {Params.BROADCAST_TOPIC});
                Utils.Wait(1000);
                if (Result.Status) break;
                Logger.Log("Main", "Failed to broadcast, retrying in 1 second...", Logger.LogLevel.WARN);
            }
            Logger.Log("Main", "Transmitted broadcast", Logger.LogLevel.INFO);

            Utils.Wait();

            Coordinator.Instance.RunCoordinator();

            Logger.Log("Main", Params.NODE_NAME +" no longer running", Logger.LogLevel.WARN);
        }
    }
}
