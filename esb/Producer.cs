using Confluent.Kafka;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Node
{
    class Producer
    {
        public static (bool Status, string[] topic) Send(string message, string[] topics)
        {

            var conf = new ProducerConfig { BootstrapServers = Params.KAFKA_BROKERS };
            bool status = true;
            List<string> failedTopics = new List<string>();
            Action<DeliveryReport<Null, string>> handler = r => {
                if (r.Error.IsError)
                {
                    Logger.Log("Producer, ", $"Delivery Error: {r.Error.Reason}", Logger.LogLevel.ERROR);
                    status = false;
                    failedTopics.Add(r.Topic);
                }
            };

            using (var p = new ProducerBuilder<Null, string>(conf).Build())
            {
                foreach (string topic in topics)
                {
                    p.Produce(topic, new Message<Null, string> { Value = message }, handler);
                }
                // wait for up to 10 seconds for any inflight messages to be delivered.
                p.Flush(TimeSpan.FromSeconds(10));

            }

            if (Logger.Levels.Contains(Logger.LogLevel.DEBUG))
            {
                var tl = topics.ToList();
                tl.AddRange(failedTopics);
                tl.Distinct();
                Logger.Log("Producer", "Send " + message + " to " + string.Join(", ", tl.Distinct()), Logger.LogLevel.DEBUG);
            }

            return (status, failedTopics.ToArray());
        }
    }
}