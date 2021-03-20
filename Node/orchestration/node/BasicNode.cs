
namespace Node 
{
    public class BasicNode
    {
        // IMPORTANT THAT THIS IS GENERATED BY NODE ITSELF
        public string ID {get; set;}
        public string Name {get; set;}
        public Job[] Jobs {get; set;}

        public static BasicNode MakeBasicNode(MetaNode metaNode)
        {
            return new BasicNode(){
                ID = metaNode.ID,
                Name = metaNode.Name,
                Jobs = metaNode.Jobs
            };
        }
    }
}