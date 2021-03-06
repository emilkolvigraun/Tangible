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

        private System.Threading.Tasks.Task OnGoing;

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
            Topics = new List<string>() {EsbVariables.REQUEST_TOPIC};
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
                OnGoing.GetAwaiter().GetResult();
            }
            token = new CancellationTokenSource();
            OnGoing = System.Threading.Tasks.Task.Run(() => {
                try 
                {
                    using (var consumer = new ConsumerBuilder<Ignore, string>(Config).Build())
                    {
                        consumer.Subscribe(Topics);

                        try 
                        {
                            Logger.Log(this.GetType().Name, "Starting listening on: " + string.Join(",", Topics.ToArray()));
                            while (true)
                            {
                                var response = consumer.Consume(token.Token);
                                consumer.Commit();
                                Request ParsedRequest = RequestUtils.DeserializeRequest(response.Message.Value.ToString());
                                RequestHandler.Instance.ProcessFromType(ParsedRequest);
                            }
                        }
                        catch (Exception)  
                        {
                            consumer.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(this.GetType().Name,string.Format("Consumer unable to connect to Kafka broker: {0}", ex.Message));
                }
            });
        }

        public void ToggleBroadcastListener()
        {
            if (Topics.Contains(EsbVariables.BROADCAST_TOPIC)) {
                Logger.Log(this.GetType().Name, "Set Broadcast Listener OFF");
                Topics.Remove(EsbVariables.BROADCAST_TOPIC);
            } 
            if (!Topics.Contains(EsbVariables.BROADCAST_TOPIC))
            {
                Logger.Log(this.GetType().Name, "Set Broadcast Listener ON");
                Topics.Add(EsbVariables.BROADCAST_TOPIC);
            }
        }

        public void Dispose()
        {
            token.Cancel();
        }
    }
}