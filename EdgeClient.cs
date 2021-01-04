using System.Collections.Generic;
using Opc.Ua.Client;
using System.Threading.Tasks;
using System;

namespace EC.MS
{
    public class EdgeClient
    {
        List<ISink> Sinks;
        List<ISource> Sources;

        ProcessingModule pModule;

        static EdgeClient Instance;

        public EdgeClient()
        {
            pModule = null;
            Sinks = new List<ISink>();
            Sources = new List<ISource>();
        }

        public void Log(string message)
        {
            DefaultLog.GetInstance().Log(LogLevel.MODULE, message);
        }

        public void AddSink(ISink sink){
            Sinks.Add(sink);
        }

        public string EdgeAddress { get; private set; }
        
        public string SendNotificationToMaster(string message)
        {
            string success = Client.NotifyServer(message);
            DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Sent notification to server: {0}", success));
            return success;
        }

        private ISink GetSinkByID(string _id)
        {
            foreach(ISink sink in Sinks)
            {
                if (sink.GetID().Equals(_id))
                {
                    return sink;
                }
            }
            return null;
        }

        private ISource GetSourceByID(string _id)
        {
            foreach(ISource source in Sources)
            {
                if (source.GetID().Equals(_id))
                {
                    return source;
                }
            }
            return null;
        }

        public string CreateOpcUaSource(string endpoint)
        {
            OpcSource source = OpcSource.CreateOpcSource(endpoint);
            Sources.Add(source);
            return source.GetID();

        }
        // KAFKA SINK FUNCTIONS - BEGIN
        public bool KafkaSendMessage(string id, string topic, string message)
        {
            KafkaSink sink = (KafkaSink) GetSinkByID(id);
            sink.SetEndpoint(topic);
            return sink.ProduceOnce(message);        
        }
        // KAFKA SINK FUNCTIONS - END

        // OPCUA SINK FUNCTIONS - START
        public string OPCUAStartSubscription(string endpoint, string nodeid, ProcessingModule onResponseHandler)
        {
            pModule = onResponseHandler;
            OpcSource source = OpcSource.CreateOpcSource(endpoint);
            bool success = false;
            try 
            {
                success = source.Consume(nodeId:nodeid, responseHandler:new MonitoredItemNotificationEventHandler(DefaultCallbackMethod));
            }
            catch (Exception e){
                Console.WriteLine(e);
            }
            if (success) {
                Sources.Add(source);
                return source.GetID();
            }
            return null;
        }

        public bool OPCUAStopSubscription(string id, string node_id = null)
        {
            OpcSource source = (OpcSource) GetSourceByID(id);
            if (node_id == null ) return source.Stop();
            else return source.Stop(reference:node_id);
        }

        public bool OPCUAWriteToNode(string endpoint, string nodeId, object value)
        {
            OpcSink sink = OpcSink.CreateOpcSink(endpoint);
            bool success = sink.ProduceOnce(nodeId, value);
            sink.Dispose();
            sink = null;
            return success;
        }

        private void DefaultCallbackMethod(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                // Task.Run(() => {
                    pModule.OnDataReceived(
                        string.Format("opcua:{0}", item.DisplayName.ToString()), 
                        string.Format("value:{0} timestamp:{1} status:{2}", 
                                        value.Value.ToString(), 
                                        (DateTime.Parse(value.SourceTimestamp.ToString()) - new DateTime(1970, 1, 1)).TotalSeconds.ToString(), 
                                        value.StatusCode.ToString())
                        );
                // });
            }
        }
        // OPCUA SINK FUNCTIONS - END


    }
}