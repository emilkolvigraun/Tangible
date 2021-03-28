using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Node
{
    class Producer
    {

        private static readonly object _producer_lock = new object();
        private static Producer _instance = null;

        public static Producer Instance 
        {
            get 
            {
                lock(_producer_lock)
                {
                    if (_instance == null) _instance = new Producer();
                    return _instance;
                }
            }
        }


        IProducer<Null, string> _producer;
        ProducerConfig config;

        Producer()
        {
            config = new ProducerConfig { BootstrapServers = Params.KAFKA_BROKERS };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public bool SendMany((string message, string topic)[] messages)
        {
            lock(_producer_lock)
            {
                try 
                {
                    List<string> failedTopics = new List<string>();

                    foreach ((string message, string topic) message in messages)
                    {
                        try 
                        {
                            _producer.Produce(message.topic, new Message<Null, string> { Value = message.message });
                        } catch(Exception e)
                        {
                            Logger.Log("SendMany", "Producer, " + e.Message, Logger.LogLevel.ERROR);
                            return false;
                        }
                    }
                    // wait for up to 10 seconds for any inflight messages to be delivered.
                    _producer.Flush(TimeSpan.FromSeconds(10));

                    return true;
                } catch(Exception e)
                {
                    Logger.Log("SendMany", "Producer, " + e.Message, Logger.LogLevel.ERROR);
                    return false;
                }
            }
        }

        public (bool Status, string[] topic) Send(string message, string[] topics)
        {
            lock(_producer_lock)
            {
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

                foreach (string topic in topics)
                {
                    _producer.Produce(topic, new Message<Null, string> { Value = message }, handler);
                }
                // wait for up to 10 seconds for any inflight messages to be delivered.
                _producer.Flush(TimeSpan.FromSeconds(10));

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

        // public static (bool Status, string[] topic) Send(string message, string[] topics)
        // {

        //     var conf = new ProducerConfig { BootstrapServers = Params.KAFKA_BROKERS };
        //     bool status = true;
        //     List<string> failedTopics = new List<string>();
        //     Action<DeliveryReport<Null, string>> handler = r => {
        //         if (r.Error.IsError)
        //         {
        //             Logger.Log("Producer, ", $"Delivery Error: {r.Error.Reason}", Logger.LogLevel.ERROR);
        //             status = false;
        //             failedTopics.Add(r.Topic);
        //         }
        //     };

        //     using (var p = new ProducerBuilder<Null, string>(conf).Build())
        //     {
        //         foreach (string topic in topics)
        //         {
        //             p.Produce(topic, new Message<Null, string> { Value = message }, handler);
        //         }
        //         // wait for up to 10 seconds for any inflight messages to be delivered.
        //         p.Flush(TimeSpan.FromSeconds(10));

        //     }

        //     if (Logger.Levels.Contains(Logger.LogLevel.DEBUG))
        //     {
        //         var tl = topics.ToList();
        //         tl.AddRange(failedTopics);
        //         tl.Distinct();
        //         Logger.Log("Producer", "Send " + message + " to " + string.Join(", ", tl.Distinct()), Logger.LogLevel.DEBUG);
        //     }

        //     return (status, failedTopics.ToArray());
        // }
    }
}