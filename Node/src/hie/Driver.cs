
namespace Node 
{
    class Driver 
    {
        public string Name {get;} = Utils.GetUniqueKey();
        public string Host {get;} = Params.DRIVER_ADVERTISED_HOST_NAME;
        public int Port {get;} = Params.UNUSED_PORT;
        private NodeClient Client {get;}

        public Driver () 
        {
            Name = Utils.GetUniqueKey();
            Host = Params.DRIVER_ADVERTISED_HOST_NAME;
            Port = Params.UNUSED_PORT;
            Client = new NodeClient(Host, Name, Port);
        }

        private readonly object _lock = new object();

        public bool TransmitRequest(Request request)
        {
            DriverRequest r0 = DriverRequest.Create(request); 
            IRequest Response = Client.Run(r0);
            return (Response.TypeOf == RequestType.STATUS && ((StatusResponse)Response).Status);
        }
    }
}