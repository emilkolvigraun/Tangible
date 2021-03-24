using System;

namespace Driver 
{
    class Driver 
    {
        public void ProcessExecute(Execute request)
        {
            // request.
            Console.WriteLine(request.EncodeRequestStr());
        }

        public void ProcessRunAs(RunAsRequest request)
        {
            // request.
            Console.WriteLine(request.EncodeRequestStr());
        }
    }
}