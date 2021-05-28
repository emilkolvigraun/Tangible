
namespace TangibleNode
{
    class Producer 
    {

        public void Broadcast()
        {
            Logger.Write(Logger.Tag.INFO, "Broadcasted to " + Params.BROADCAST_TOPIC);
        }

        public void Respond(string message, string topic)
        {
            Logger.Write(Logger.Tag.INFO, "Responded to " + topic);
        }

        //-------------------CONSTRUCTER------------------//
        private static readonly object _lock = new object();
        private static Producer _instance = null;
        public static Producer Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance==null)_instance=new Producer();
                    return _instance;
                }
            }
        }
    }
}