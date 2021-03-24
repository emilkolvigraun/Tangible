
namespace Driver 
{
    class ProcessLoop
    {
        private static ProcessLoop _instance = null;
        private static readonly object _lock = new object();
    
        public static ProcessLoop Instance 
        {
            get 
            {
                lock(_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ProcessLoop();
                    }
                    return _instance;
                }
            }
        }

        public void Run(Driver driver)
        {
            while (true)
            {
                IRequest exe = ProcessQueue.Instance.Dequeue();
                if (exe != null)
                {
                    if (exe.TypeOf == RequestType.HI)
                    {
                        driver.ProcessExecute((Execute)exe);
                    } else if (exe.TypeOf == RequestType.RN)
                    {
                        driver.ProcessRunAs((RunAsRequest)exe);
                    }
                }
            }
        }
    }
}