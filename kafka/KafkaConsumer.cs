using System.Collections.Generic;
using System.Threading;
using System;
using Confluent.Kafka;
using System.Threading.Tasks;

namespace Node
{
    class KafkaConsumer
    {

        public ConsumerConfig Config { get; private set; }
        private CancellationTokenSource token;
        private static KafkaConsumer _instance = null;
        private static readonly object padlock = new object();
        private List<string> Topics;
        private IConsumer<Ignore, string> Consumer = null;
        private System.Threading.Tasks.Task OnGoing;
        public bool subscribing {get; private set;} = false;

        public static KafkaConsumer Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new KafkaConsumer();
                    }
                    return _instance;
                }
            }
        }

        KafkaConsumer()
        {
            Topics = new List<string>() {EsbVariables.REQUEST_TOPIC, EsbVariables.BROADCAST_TOPIC};
            Config = new ConsumerConfig 
            {
                GroupId = EsbVariables.CLUSTER_ID,
                BootstrapServers = EsbVariables.KAFKA_BROKERS,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                AllowAutoCreateTopics = true
            };
        }

        public void Subscribe()
        {
            if (OnGoing != null)
            {
                Dispose();
                // OnGoing.GetAwaiter().GetResult();
            }

            if (Consumer != null)
            {
                Consumer.Commit();
                Consumer.Unsubscribe();
                Consumer.Close();
                Consumer.Dispose();
            }

            token = new CancellationTokenSource();
            OnGoing = System.Threading.Tasks.Task.Run(() => {
                try 
                {
                    Consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                    subscribing = true;
                    Consumer.Subscribe(Topics);
                    try 
                    {
                        Logger.Log(this.GetType().Name, "Subscribed to: " + string.Join(",", Topics.ToArray()), Logger.LogLevel.INFO);
                        while (true)
                        {
                            var response = Consumer.Consume(token.Token);
                            Consumer.Commit();
                            Request ParsedRequest = RequestUtils.DeserializeRequest(response.Message.Value.ToString());
                            RequestHandler.Instance.ProcessFromType(ParsedRequest);
                        }
                    }
                    catch (Exception)  
                    {   
                        try {
                            Consumer.Commit();
                            Consumer.Unsubscribe();
                            Consumer.Close();
                        } catch (Exception) {}
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(this.GetType().Name,string.Format("Consumer unable to connect to Kafka broker: {0}", ex.Message), Logger.LogLevel.ERROR);
                }
            });
        }

        public void Dispose()
        {
            try
            {
                token.Cancel();
                subscribing = false;
                // OnGoing.GetAwaiter().GetResult();
            } catch (Exception) {}
        }
    }
}