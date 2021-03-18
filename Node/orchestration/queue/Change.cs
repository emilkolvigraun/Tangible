
namespace Node 
{
    public class Change 
    {
        public enum Type 
        {
            DEL,
            ADD
        }

        public Type TypeOf {get; set;}

        public string Name {get; set;}
        public string Host {get; set;}
        public int Port {get; set;}
    }
}