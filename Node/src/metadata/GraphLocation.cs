
namespace Node 
{
    public class GraphLocation 
    {
        public string ID {get; set;}
        public GraphLocation LocationOf {get; set;}
        public GraphPoint HasPoint {get; set;}
    }
}