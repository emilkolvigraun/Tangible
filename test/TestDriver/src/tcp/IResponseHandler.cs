
namespace TangibleDriver
{
    public interface IResponseHandler 
    {
        void OnResponse(string receiverID, RequestBatch sender, string response);
    }
}