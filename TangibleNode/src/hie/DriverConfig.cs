
namespace TangibleNode
{
    class DriverConfig
    {
        public string ID {get; set;}
        public string Host {get; set;}
        public int Port {get; set;}
        public int Replica {get; set;}
        public string Image {get; set;}
        
        public Credentials AssociatedNode {get; set;}
    }
}