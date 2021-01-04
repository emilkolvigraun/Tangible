namespace EC.MS 
{
    public interface ISink
    {
        string GetID();    
        void SetEndpoint(string endpoint);
        bool ProduceOnce(string message, object reference = null);
        bool ProduceSeveral(string[] message, object reference = null);

        void Dispose();
    }
}