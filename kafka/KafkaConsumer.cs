using System.Collections.Generic;
using System.Threading;
using System;
using Confluent.Kafka;
using System.Threading.Tasks;

namespace EC.MS
{
    class KafkaConsumer
    {

        public ConsumerConfig Config { get; private set; }
        private CancellationTokenSource token;

        public KafkaConsumer(string servers, string group_id, AutoOffsetReset offset = AutoOffsetReset.Latest)
        {
            Config = new ConsumerConfig 
            {
                GroupId = group_id,
                BootstrapServers = servers,
                AutoOffsetReset = offset,
                EnableAutoCommit = true,
            };

            token = new CancellationTokenSource();
            DefaultLog.GetInstance().Log(LogLevel.INFO, "Created new Kafka consumer.");
        }

        public Task Subscribe(IEnumerable<string> topics, ProcessingModule module)
        {  
            Task t = Task.Run(() => {
                try 
                {
                    using (var consumer = new ConsumerBuilder<Ignore, string>(Config).Build())
                    {
                        consumer.Subscribe(topics);

                        try 
                        {
                            DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Started subscribing to Kafka topic(s): {0}", string.Join(",", topics) ));
                            while (true)
                            {
                                var response = consumer.Consume(token.Token);
                                consumer.Commit();
                                bool success = module.OnDataReceived("kafka:"+response.Topic, response.Message.Value);
                                DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Result from processing module: {0}", success));
                            }
                        }
                        catch (Exception ex)  
                        {
                            DefaultLog.GetInstance().Log(LogLevel.ERROR, string.Format("Failed to subscribe to: {0}:{1}, Exception: {2}", Config.BootstrapServers.ToString(), string.Join(",", topics), ex.Message));
                            consumer.Close();
                            //Client.NotifyServer("Kafka Consumer was closed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DefaultLog.GetInstance().Log(LogLevel.ERROR, string.Format("Consumer unable to connect to Kafka broker: {0}", ex.Message));
                }

            });
            return t;
        }

        public void Dispose()
        {
            token.Cancel();
        }

    }
}