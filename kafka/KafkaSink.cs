using System;
using System.Threading.Tasks;

namespace EC.MS
{
    class KafkaSink : ISink
    {
        private string ID;
        private string Topics;
        private KafkaProducer Producer;

        public KafkaSink(string _brokers, string _id){
            Producer = KafkaProducer.CreateProducer(_brokers);
            ID = _id;
            DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Initialized KafkaSink to: {0}", _brokers));
        }

        public string GetID(){
            return ID;
        }

        public void SetEndpoint(string endpoint)
        {
            Topics = endpoint;
        }

        public bool ProduceOnce(string message, object reference = null) 
        {
            if (Topics != null)
            {
                try
                {
                    // Task.Run(() => Producer.SendMessage(new string[] {message}, Topics));
                    Producer.SendMessage(new string[] {message}, Topics);
                    return true;
                } 
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        public bool ProduceSeveral(string[] messages, object reference = null)
        {
            return false;
        }

        public void Dispose()
        {
            
        }
    }
}