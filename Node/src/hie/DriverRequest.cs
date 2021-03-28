
namespace Node 
{
    class DriverRequest : IRequest
    {
        public RequestType TypeOf {get; set;} = RequestType.EXECUTE;
        public string ID {get; set;}
        public ActionType Action {get; set;}
        public string Value {get; set;}
        public string PointID {get; set;}

        public static DriverRequest Create(Request request)
        {
            return new DriverRequest(){
                Action = request.TypeOf,
                ID = request.ID,
                Value = request.Value,
                PointID = request.PointID
            };
        }
    }
}