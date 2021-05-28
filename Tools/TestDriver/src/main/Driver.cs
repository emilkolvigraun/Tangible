using System.Threading;

namespace TangibleDriver 
{
    class Driver
    {

        private AsynchronousSocketListener _listener {get;}
        private Thread _handlerThread {get;}

        public Driver (IRequestHandler RequestHandler)
        {
            Params.LoadEnvironment();

            MainLoop _handler = new MainLoop();
            _listener = new AsynchronousSocketListener(_handler);

            _handlerThread = new Thread(() => {
                _handler.Start(RequestHandler);
            });
        }

        public void Start()
        {
            _handlerThread.Start();
            _listener.StartListening();
        }
    }
}