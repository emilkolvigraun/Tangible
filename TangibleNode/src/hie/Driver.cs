
namespace TangibleNode
{
    class Driver 
    {
        public DriverConfig Config {get;}
        public string Image {get;}
        private SynchronousClient Client {get;}

        public Driver(DriverConfig Config, string Image)
        {
            this.Config = Config;
            this.Image = Image;
            Client = new SynchronousClient(Config.Host, Config.Port, Config.ID);
        }

        public void Write(Action action, IResponseHandler rh)
        {
            Client.StartClient(new RequestBatch(){

            }, rh);
        }

        public static Driver MakeDriver(string image, int replica)
        {
            string name = Params.ID+"_"+image.Replace("-","_").Replace("/","_").Replace(" ", "")+"_"+replica.ToString();
            DriverConfig config = new DriverConfig(){
                ID = name,
                Host = Params.DOCKER_HOST,
                Port = Params.GetUnusedPort(),
                Maintainer = Node.Self,
                Image = image
            };
            Docker.Instance.Containerize(config).GetAwaiter().GetResult();
            return new Driver(config, image);
        }

        public static Driver MakeDriver(Action action, int replica)
        {
            return MakeDriver(action.Image, replica);
        }
    }
}