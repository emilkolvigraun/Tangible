
namespace Node 
{
    public class Job 
    {
        public enum Type
        {
            SD, // shadow operate
            OP // operative
        }
        public enum Status
        {
            CP, // Complete
            OG, // Ongoing
            NS // Not started
        }
        public string ID {get;} = Utils.GetUniqueKey(size:10);
        public Status StatusOf {get; set;}
        
        public Type TypeOf {get; set;}

        public ActionRequest Request {get; set;}
    }
}