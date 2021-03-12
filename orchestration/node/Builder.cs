
namespace Node 
{
    public class Builder
    {
        public static BasicNode CreateBasicNode(string name, string host, int port)
        {
            return new BasicNode(){
                Host = host,
                Port = port,
                Name = name
            };
        }

        public static MetaNode CreateMetaNode(string name, string host, int port, double usage, BasicNode[] nodes)
        {
            return new MetaNode(){
                Host = host,
                Port = port,
                Name = name,
                Nodes = nodes,
                Usage = usage
            };
        }
    }
}