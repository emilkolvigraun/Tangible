
namespace Node 
{
    class VoteNode : INode 
    { 
        public string Host {get; set;} = Params.ADVERTISED_HOST_NAME;
        public int Port {get; set;} = Params.PORT_NUMBER;
        public string Name {get; set;} = Params.NODE_NAME;
        public double Usage {get; set;} = Params.USAGE;
    }
}