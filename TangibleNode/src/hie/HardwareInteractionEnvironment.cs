
namespace TangibleNode
{
    class HardwareInteractionEnvironment
    {
        private TDict<string, Driver> _drivers = new TDict<string, Driver>();

        public void PrepareWarmStart(string image)
        {
            Logger.Write(Logger.Tag.INFO, "Removing dangeling containers...");
            Docker.Instance.RemoveStoppedContainers().GetAwaiter().GetResult();
            Logger.Write(Logger.Tag.INFO, "Preparing warm start...");
            if (!_drivers.ContainsKey(image))
            {
                // create driver
                _drivers.Add(image, Driver.MakeDriver(image, _drivers.Count));
            }
        }

        public Driver GetOrCreateDriver(Action action)
        {
            if (!_drivers.ContainsKey(action.Image))
            {
                _drivers.Add(action.Image, Driver.MakeDriver(action, _drivers.Count));
                // create driver
            } 
            return _drivers[action.Image];
        }
        
    }
}