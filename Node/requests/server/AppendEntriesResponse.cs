
namespace Node
{
    class AppendEntriesResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AR;
        // public MetaNode Node {get; set;} = new MetaNode();

        // returns the ID of each node
        public string[] NodeIds {get; set;}

        // returns the IDs of each job
        public string[] JobIds {get; set;} 
    }
}