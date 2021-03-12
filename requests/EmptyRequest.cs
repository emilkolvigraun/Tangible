

namespace Node 
{
    class EmptyRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.EMPTY;
    }
}