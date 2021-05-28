
namespace TangibleNode
{
    class Vote 
    {
        public string ID {get; set;}

        // DEBUGGING
        public int LogCount {get; set;}
        public SyncParams Parameters {get; set;} = Params.AsParams;

        //TODO: IMPLEMENT EXCHANGE IN THE CONTENT OF STATELOGS
    }
}