using System.Threading;

namespace TangibleNode
{
    class TangibleNode
    {

        AsynchronousSocketListener _listener {get;}
        Thread _stateMachineThread {get;}
        Thread _consumerThread {get;}

        public TangibleNode(Settings settings)
        {
            Params.LoadEnvironment(settings);

            // initializing the listener
            _listener = new AsynchronousSocketListener();

            // initializing the HA and loading the RDF model
            HardwareAbstraction HA = new HardwareAbstraction(settings.RDFPath);

            // initializing the consumer and providing the HA
            Consumer consumer = new Consumer(HA);

            // initializing the node itself [as a statemachine]
            StateMachine _stateMachine = new StateMachine(consumer);

            _stateMachineThread = new Thread(() => {
                _stateMachine.Start(settings.TcpNodes);
            });
            _consumerThread = new Thread(() => {
                consumer.Start();
            });
        }

        public void Start()
        {   
            _stateMachineThread.Start();
            _consumerThread.Start();
            _listener.StartListening();
        }
    }
}