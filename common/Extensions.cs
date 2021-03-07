using System.Collections.Generic;

namespace Node
{
    static class Extensions
    {

        public static Description AsDescription(this MetaNode metaNode, string CN)
        {
            return new Description(){
                CommonName = CN,
                AdvertisedHostName = metaNode.AdvertisedHostName,
                Port = metaNode.Port,
                Workload = -1,
                HeartBeat = -1,
                Quorum = new Dictionary<string, QuorumNode>(),
                Tasks = new List<Task>()
            };
        }

        public static QuorumNode AsQuorum(this Description description)
        {
            return new QuorumNode(){
                CommonName = description.CommonName,
                AdvertisedHostName = description.AdvertisedHostName,
                Port = description.Port,
                Workload = description.Workload,
                HeartBeat = description.HeartBeat,
                Quorum = AsMetaQuorum(description.Quorum),
            };
        }

        public static Dictionary<string, MetaNode> AsMetaQuorum(this Dictionary<string, QuorumNode> quorum)
        {
            Dictionary<string, MetaNode> MetaQuorum = new Dictionary<string, MetaNode>();
            foreach(KeyValuePair<string, QuorumNode> qn in quorum)
                MetaQuorum.Add(qn.Key, new MetaNode(){
                    AdvertisedHostName = qn.Value.AdvertisedHostName,
                    Port = qn.Value.Port
                });
            return MetaQuorum;
        }

        public static DataObject AsDataObject(this QuorumNode quorumNode)
        {
            return new DataObject(){
                Key = quorumNode.AdvertisedHostName,
                Value = new DataObject(){
                    Key = quorumNode.Port.ToString()
                }
            };
        }
    }
}