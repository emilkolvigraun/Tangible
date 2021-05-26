using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TangibleNode
{
    class NodePeers_test
    {
        public static void Run()
        {
            NodePeers Nodes = new NodePeers();

            List<Task> tasks = new List<Task>();
            int i = 0;
            while (i<10)
            {
                string ID = i.ToString();
                string Host = i.ToString()+"."+i.ToString()+"."+i.ToString()+"."+i.ToString();
                int Port = int.Parse(i.ToString()+i.ToString()+i.ToString()+i.ToString());
                tasks.Add(new Task(()=>{
                    Nodes.AddNewNode(new Sender(){
                        ID = ID,
                        Host = Host,
                        Port = Port
                    });
                }));
                i++;
            }

            Task[] _tasks = tasks.ToArray();
            Parallel.ForEach<Task>(_tasks, (t) => { t.Start(); });
            Task.WhenAll(_tasks).ContinueWith(t => {
                if (Nodes.NodeCount != i) throw new Exception("NODES WAS NOT ADDED");
            });
            Task.WaitAll(_tasks);
            Console.WriteLine("Nodes_test add successful " + Nodes.NodeCount);

            Nodes.AddNewNode(new Sender(){
                ID = "0",
                Host = "test123",
                Port = 1234
            });

            Node client;
            Nodes.TryGetNode("0", out client);

            if (client.Client.Host!="test123") throw new Exception("NODE WAS NOT UPDATED");
            Console.WriteLine("Nodes_test replace successful " + Nodes.NodeCount);
            
            List<Task> tasks1 = new List<Task>();
            int j = 0;
            while (j<10)
            {
                string ID = j.ToString();
                tasks1.Add(new Task(()=>{
                    Node client;
                    Nodes.TryGetNode(ID, out client);
                    Nodes.TryRemoveNode(ID);
                }));
                j++;
            }

            Task[] _tasks1 = tasks1.ToArray();
            Parallel.ForEach<Task>(_tasks1, (t) => { t.Start(); });
            Task.WhenAll(_tasks1).ContinueWith(t => {
                if (Nodes.NodeCount != 0) throw new Exception("NODES WAS NOT REMOVED");
            });
            Task.WaitAll(_tasks1);


            Console.WriteLine("Nodes_test remove successful " + Nodes.NodeCount);
        }
    }
}