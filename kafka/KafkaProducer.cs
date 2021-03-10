using System;
using Confluent.Kafka;

namespace Node
{ 
    class KafkaProducer
    {
        public static void SendMessage(Request Request)
        {

            ProducerConfig Config = new ProducerConfig { 
                BootstrapServers = EsbVariables.KAFKA_BROKERS,
                ClientId = Orchestrator.Instance._Description.CommonName,
                Acks = Acks.Leader
            };

            using (var p = new ProducerBuilder<Null, string>(Config).Build())
            {
                
                try
                { 
                    string msg = RequestUtils.SerializeRequest(Request);
                    p.Produce(EsbVariables.BROADCAST_TOPIC, new Message<Null, string> { Value = msg });
                    Logger.Log("KafkaProducer", msg, Logger.LogLevel.INFO);
                }
                catch (ProduceException<Null, string> e)
                {
                    Logger.Log("KafkaProducer",$"Kafka message production failed: {e.Error.Reason}", Logger.LogLevel.ERROR);
                }
                
                p.Flush(TimeSpan.FromSeconds(10));
                p.Flush();
                p.Dispose();
            }
        }
    }
}
