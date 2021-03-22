using System.Collections.Generic;

namespace Node
{
    class AppendEntriesResponse : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.AR;
        // public MetaNode Node {get; set;} = new MetaNode();

        // // returns the ID of each node
        // public string[] NodeIds {get; set;}

        // returns the IDs of each job
        public string[] JobIds {get; set;} 
        public (string node, string[] jobs)[] SyncRequest {get; set;}
        public (string node, Job[] jobs)[] SyncResponse {get; set;}

        // Returns the ID of each node and the id of the associated jobs
        public (string node, string[] jobIds)[] Ledger {get; set;}
    }
}