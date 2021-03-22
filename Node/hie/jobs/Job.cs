
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
        public string ID {get; set;}
        public Status StatusOf {get; set;}
        
        public Type TypeOf {get; set;}

        public ActionRequest Request {get; set;}

        public (Job Operational, Job Shadow) MakeJob(ActionRequest request)
        {
            Job op = null;
            Job sh = null;
            if (request.TypeOf == RequestType.SUBSCRIBE)
            {

            } else 
            {

            }
            return (op,sh);
        }
    }
}