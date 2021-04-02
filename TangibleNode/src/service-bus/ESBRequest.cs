
namespace TangibleNode
{
    class ESBRequest
    {
        public enum _Type 
        {
            ACTION, 
            BROADCAST
        }
        public _Type Type {get; set;}
        public string Data {get; set;}
    }
}