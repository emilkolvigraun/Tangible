
namespace TangibleDriver
{
    public interface IResponseHandler 
    {
        void OnResponse(string receiverID, ProcedureCallBatch sender, string response);
    }
}