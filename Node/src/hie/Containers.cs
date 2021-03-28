using System.Collections.Generic;
using System.Threading.Tasks;

namespace Node 
{
    class Containers
    {
        private Dictionary<string, Driver> _Drivers = new Dictionary<string, Driver>();
        private static readonly object _lock = new object();
        private static Containers _instance = null;
        public static Containers Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if(_instance==null)_instance=new Containers();
                    return _instance;
                }
            }
        }
        private bool busy = false;
        public bool IsBusy
        {
            get 
            {
                lock(_lock)
                {
                    return busy;
                }
            }
        }
        public void CreateContainer(string image)
        {
            lock(_lock)
            {
                if(busy) return;
                busy = true;
            }
            Task.Run(async()=>{

                Driver driver = new Driver();
                _ = await DockerAPI.Instance.Containerize(image, driver.Host, driver.Port, driver.Name, 0);
                lock(_lock)
                {
                    this._Drivers.Add(image, driver);
                }
                busy = false;
            });
        }
    
        public bool ContainsDriver(string image)
        {
            lock(_lock)
            {
                return _Drivers.ContainsKey(image);
            }
        }

        public bool ExecuteRequest(Request request)
        {
            lock (_lock)
            {
                if (!ContainsDriver(request.Image))
                {
                    if(!IsBusy) CreateContainer(request.Image);
                    return false;
                }

                return _Drivers[request.Image].TransmitRequest(request);
            }
        }
    }
}