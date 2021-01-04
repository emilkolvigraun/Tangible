using System;
using System.Linq;
using System.Collections.Generic;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Confluent.Kafka;

namespace EC.MS
{ 
    class KafkaProducer
    {
        private ProducerConfig Config;
        private string Servers;

        public static KafkaProducer CreateProducer(string servers){
            return new KafkaProducer(servers);
        }

        public KafkaProducer(string servers)
        { 
            Servers = servers;
            // Producer producer = new Producer( Routes );
            // List<Message> messages = new List<Message> { new Message(message) };
            // await producer.SendMessageAsync(topic:topic, messages:messages);
            // producer.Stop();
            // Console.WriteLine("sent {" + message + "} to " + topic);
            Config = new ProducerConfig { 
                BootstrapServers = Servers,
                ClientId = Utils.GetUniqueKey(10),
                Acks = Acks.Leader
            };
            DefaultLog.GetInstance().Log(LogLevel.INFO, "Created new Kafka producer.");
        }

        public void SendMessage(string[] messages, string topic)
        {
            // If serializers are not specified, default serializers from
            // `Confluent.Kafka.Serializers` will be automatically used where
            // available. Note: by default strings are encoded as UTF8.
            using (var p = new ProducerBuilder<Null, string>(Config).Build())
            {
                foreach(string msg in messages)
                {
                    try
                    { 
                        p.Produce(topic, new Message<Null, string> { Value = msg });
                        DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Produced Kafka message to: {0}", $"'{topic}'"));
                    }
                    catch (ProduceException<Null, string> e)
                    {
                        DefaultLog.GetInstance().Log(LogLevel.ERROR, $"Kafka message production failed: {e.Error.Reason}");
                    }
                }
                p.Flush(TimeSpan.FromSeconds(10));
                p.Dispose();
            }
            
        }
    }
}
