
namespace Node 
{
    class JobAppendRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.HI;

        public Execute[] Executives {get; set;}
    }
}