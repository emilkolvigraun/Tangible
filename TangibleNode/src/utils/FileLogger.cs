using System.Threading.Tasks;
using System.IO;

namespace TangibleNode
{
    class FileLogger 
    {

        private string Path {get; set;}
        private static readonly object _lock = new object();
        private static FileLogger _instance = null;

        public bool IsEnabled
        {
            get 
            {
                lock(_lock)
                {
                    return _fileLog;
                }
            }
        }

        private bool _fileLog = false;
        public static void EnableFileLog()
        {
            lock(_lock)
            {
                Instance._fileLog=true;
                Logger.Write(Logger.Tag.INFO, "File-Logger enabled -> " + Params.ID + ".txt");
            }
        }
        
        public static FileLogger Instance 
        {
            get 
            {
                if (_instance==null) _instance=new FileLogger(){
                    Path = Params.ID+".txt"
                };
                return _instance;
            }
        }

        public void WriteHeader()
        {
            if (Instance._fileLog)
            {
                WriteToFile("time,action_count,log_count,priority_queue_count,node_count,state,cpu_time,memory_usage");
            }
        }

        public void CreateLogFile()
        {
            if (Instance._fileLog)
            {
                if (!File.Exists(Path))
                    File.Create(Path).Close();
            }
        }

        private bool _isWriting = false;
        private TQueue<string> _writeQueue = new TQueue<string>();
        public void WriteToFile(string text)
        {
            if (_fileLog)
            {
                if (!_isWriting)
                {
                    _writeQueue.Enqueue(text);
                    Task.Run(()=>{
                        _isWriting=true;
                        using (StreamWriter sw = File.AppendText(Path))
                        {
                            while (true)
                            {
                                if (_writeQueue.Count<1) break;
                                sw.WriteLine(_writeQueue.Dequeue());
                            }
                        }
                        _isWriting=false;
                    });
                } else 
                {
                    _writeQueue.Enqueue(text);
                }
            }	
        }
    }
}