using Opc.Ua;
using Opc.Ua.Client;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EC.MS 
{
    class OpcSink : ISink
    {
        private string ID;
        private string Endpoint;
        private OpcClient Client;

        public static OpcSink CreateOpcSink(string endpoint){
            DefaultLog.GetInstance().Log(LogLevel.INFO, string.Format("Trying to create OpcSink to: {0}", endpoint));
            return new OpcSink(Utils.GetUniqueKey(10), endpoint);
        }

        public OpcSink(string id, string endpoint){
            ID = id;
            Client = new OpcClient(endpoint);
        }

        public string GetID(){
            return ID;
        }
        public void SetEndpoint(string endpoint) 
        {
            Endpoint = endpoint;
        }

        public bool ProduceOnce(string reference, object message = null)
        {
            foreach(ReferenceDescription _ref in Client.BrowseObjectsNode()){
                if (_ref.BrowseName.ToString().Contains(reference) || _ref.NodeId.ToString().Contains(reference))
                {
                    return Client.WriteToNode(_ref, message);
                }
            }
            return false;
        }

        public bool ProduceSeveral(string[] message, object reference = null)
        {
            return true;
        }

        public void Dispose()
        {
            Client.Disconnect();
        }
    }
}