using System.Linq;

namespace TangibleNode
{
    class HardwareInteractionEnvironment
    {
        private TDict<string, Driver> _drivers = new TDict<string, Driver>();

        public void PrepareWarmStart(string image)
        {
            if (!_drivers.ContainsKey(image))
            {
                // create driver
                _drivers.Add(image, Driver.MakeDriver(image, _drivers.Count));
            }
        }

        public void ForEachDriver(System.Action<Driver> action)
        {
            _drivers.Values.ToList().ForEach((d) => {
                action(d);
            });
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