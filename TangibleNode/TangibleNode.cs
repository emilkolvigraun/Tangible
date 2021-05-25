using System.Threading;using System;
using System.IO;
using Newtonsoft.Json;

namespace TangibleNode
{
    class TangibleNode
    {

        ///<summary>
        ///<para>Initialization fo the server-thread.</para>
        ///</summary>
        AsynchronousSocketListener _listener {get;}

        ///<summary>
        ///<para>Initialization of the SM, and thus, the event-loop-thread</para>
        ///</summary>
        Thread _stateMachineThread {get;}

        ///<summary>
        ///<para>Initialization of the consumer-thread.</para>
        ///</summary>
        Thread _consumerThread {get;}

        ///<summary>
        ///<para>A tangible node is started with input params from standard-in to localize config.</para>
        ///<para>Ex. > dotnet run Program.cs filepath</para>
        ///</summary>
        public TangibleNode(string[] args)
        {
            // USED FOR DEBUGGING/EVALUATION
            bool _enableStateLog = false;

            Settings settings = default(Settings);            

            if (args.Length > 0)
            {
                try
                {
                    // load the configuration file by the specified filepath
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]));
                    
                    // OPTIONAL ARE ORGANIZATION EMPHASISING EVALUATION
                    if (settings.Optional==null) settings.Optional = new Optional();
                    if (args.Length > 2)
                    {
                        settings.ID = args[2];
                    }
                    
                    // DEBUGGING
                    Console.WriteLine(args[0] +" : "+JsonConvert.SerializeObject(settings, Formatting.Indented));
                    
                    // write the header, so that when the log is piped, the columns are given
                    Logger.WriteHeader();
                    Logger.Write(Logger.Tag.INFO, "Loaded settings.");
                } catch
                {
                    // exit if there are no settings provided
                    Logger.Write(Logger.Tag.FATAL, "Failed to load settings.");
                }
            } else Logger.Write(Logger.Tag.INFO, "No settings provided.");

            // DEBUGGING/EVALUATION
            Utils.Sleep(settings.Optional.WaitBeforeStart_MS);

            // parse the settings
            Params.LoadEnvironment(settings);

            // DEBUGGING/EVALUATION
            if (args.Length > 1)
            {
                _enableStateLog = bool.Parse(args[1]);
                if (_enableStateLog)
                {
                    FileLogger.EnableFileLog();
                }
            }

            FileLogger.Instance.CreateLogFile();
            FileLogger.Instance.WriteHeader();

            // initializing the listener
            _listener = new AsynchronousSocketListener();

            // initializing the HA and loading the RDF model
            HardwareAbstraction ha = new HardwareAbstraction(settings.RDFPath);

            HardwareInteractionEnvironment hie = new HardwareInteractionEnvironment();

            // prepare warm start
            ha.LoadDriverImages(hie);
            
            // initializing the consumer and providing the HA
            Consumer consumer = new Consumer(ha);

            // initializing the node itself [as a statemachine]
            StateMachine _stateMachine = new StateMachine(consumer, hie);

            _stateMachineThread = new Thread(() => {
                _stateMachine.Start(settings.TcpNodes);
            });
            _consumerThread = new Thread(() => {
                consumer.Start();
            });
        }

        ///<summary>
        ///<para>Starts the event-loop-, consumer- and server-thread.</para>
        ///</summary>
        public void Start()
        {   
            _stateMachineThread.Start();
            _consumerThread.Start();
            _listener.StartListening();
        }
    }
}