using System;
using System.Collections.Generic;

namespace TangibleNode
{
    class TQueue_test 
    {
        public static void Run()
        {
            PQueue q = new PQueue();

            int i = 0;
            while (i<10)
            {
                q.Enqueue(
                    new DataRequest(){
                        Type = DataRequest._Type.WRITE,
                        PointDetails = new Dictionary<string, List<string>> {{"awdawdadwawdawd", new List<string>{"sensor_999"}}},
                        Image = "docker-image-1",
                        Priority = i
                    }
                );
                i++;
            }

            for(int j = 0; j<i;j++)
                if(q.PCount(j) != 1) throw new Exception("ENTRY WAS NOT ADDED");

            DataRequest a = q.Dequeue();
            int w = i-1;
            while (a!=null)
            {
                if(a.Priority!=w) throw new Exception("DEQUEUED IN WRONG ORDER");
                a = q.Dequeue();
                w--;;
            }
            if(q.Count!=i) throw new Exception("PRIORITY WAS NOT PERSISTED");

            Console.WriteLine("PQUEUE test successful");
        }
    }
}