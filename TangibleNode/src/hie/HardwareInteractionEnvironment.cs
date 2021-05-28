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

        public List<Driver> GetOrCreateDriver(DataRequest action, bool wait = false)
        {
            List<Driver> drs = new List<Driver>();
            foreach(KeyValuePair<string, List<string>> details in action.PointDetails)
            {
                if (!_drivers.ContainsKey(details.Key))
                {
                    // create driver
                    Create(details.Key, wait);
                } 

                Driver d0 = _drivers[details.Key].ElementAt(0);

                // extract the least busy driver
                foreach (Driver d1 in _drivers[details.Key].ToList())
                {
                    if (d1.BehindCount < d0.BehindCount)
                    {
                        d0 = d1;
                    }
                }

                if (d0.BehindCount > 500)
                {
                    Create(details.Key, false, _drivers[details.Key].Count);
                }
                drs.Add(d0);
            }
            return drs;
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