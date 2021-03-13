using System;
using System.Threading.Tasks;
using System.Threading;
using Confluent.Kafka;

namespace Node
{
    class Consumer
    {
        private CancellationTokenSource tSrc;
        private EsbRequestHandler Handler = new EsbRequestHandler();
        private bool _running = false;
        public void Start(string[] topics)
        {
            if (_running) return;
            var config = new ConsumerConfig
            {
                BootstrapServers = Params.KAFKA_BROKERS,
                GroupId = Params.CLUSTER_ID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                AllowAutoCreateTopics = true,
                // SessionTimeoutMs = 60000,
                // MaxPollIntervalMs = 65000
            };
            tSrc = new CancellationTokenSource();
            Consume(topics, config, tSrc.Token);
            _running = true;
        }

        async private void Consume(string[] topics, ConsumerConfig config, CancellationToken token)
        {
            await Task.Run(() => {
                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe(topics);

                    Logger.Log(this.GetType().Name, "Started ESB consumer on: " + string.Join(", ", topics), Logger.LogLevel.INFO);
                    try 
                    {
                        while (!token.IsCancellationRequested)
                        {
                            Logger.Log(this.GetType().Name, "Waiting for next message", Logger.LogLevel.DEBUG);
                            var consumeResult = consumer.Consume(tSrc.Token);
                            consumer.Commit();
                            
                            // handle consumed message.
                            Logger.Log(this.GetType().Name, consumeResult.Message.Value, Logger.LogLevel.DEBUG);
                            try 
                            {
                                Handler.ProcessRequest(consumeResult.Message.Value);
                            } catch (Exception)
                            {
                                Logger.Log(this.GetType().Name, "Received malformed request", Logger.LogLevel.WARN);
                            }
                        }
                        Clean(consumer);
                    } 
                    catch (OperationCanceledException) 
                    {
                        Clean(consumer);
                        Logger.Log(this.GetType().Name, "threw OperationCanceledException", Logger.LogLevel.DEBUG);
                    }
                }
            });
            Logger.Log(this.GetType().Name, "Consumer has stopped", Logger.LogLevel.WARN);
        }

        private void Clean(IConsumer<Ignore, string> consumer)
        {
            consumer.Unsubscribe();
            try
            {
                consumer.Commit();
            } catch (KafkaException){ }
            consumer.Close();
            consumer.Dispose();
        }
        public void Stop()
        {
            try 
            {
                if(tSrc != null)tSrc.Cancel();
            } catch (Exception e)
            {
                Logger.Log("Consumer", e.Message, Logger.LogLevel.ERROR);
            }
            _running = false;
        }

        private static readonly object _lock = new object();
        private static Consumer _instance = null;
        public static Consumer Instance 
        {
            get 
            {
                lock (_lock)
                {
                    if (_instance == null) _instance = new Consumer();
                    return _instance;
                }
            }
        }
    }
}