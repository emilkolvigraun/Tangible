using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TangibleNode
{
    class HardwareInteractionEnvironment
    {
        private TDict<string, HashSet<Driver>> _drivers = new TDict<string, HashSet<Driver>>();

        public void PrepareWarmStart(string image)
        {
            if (!_drivers.ContainsKey(image))
            {
                // create driver
                Create(image, true, 0);
            }
        }

        public void ForEachDriver(System.Action<Driver> action)
        {
            _drivers.Values.ToList().ForEach((h) => {
                foreach (Driver d in h)
                {
                    action(d);
                }
            });
        }

        public Driver GetOrCreateDriver(Action action, bool wait = false)
        {
            if (!_drivers.ContainsKey(action.Image))
            {
                // create driver
                Create(action.Image, wait);
            } 

            Driver d0 = _drivers[action.Image].ElementAt(0);

            // extract the least busy driver
            foreach (Driver d1 in _drivers[action.Image].ToList())
            {
                if (d1.BehindCount < d0.BehindCount)
                {
                    d0 = d1;
                }
            }

            if (d0.BehindCount > 500)
            {
                Create(action.Image, false, _drivers[action.Image].Count);
            }

            return d0;
        }

        private void Create(string image, bool wait = false, int replica = 0)
        {
            Task t = Task.Run(() => {
                _drivers.Add(image, new HashSet<Driver>{Driver.MakeDriver(image, replica)});
            });

            if (wait)
            {
                t.Wait();
            }
        }
        
    }
}