
namespace Server 
{
    public interface IRequest
    {
        // base request
        RequestType TypeOf {get; set;}
    }
}