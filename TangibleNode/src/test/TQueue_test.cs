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
                    new Action(){
                        Type = Action._Type.WRITE,
                        PointID = new List<string>{"123abc", "321cba"},
                        Image = "docker-image-1",
                        Priority = i
                    }
                );
                i++;
            }

            for(int j = 0; j<i;j++)
                if(q.PCount(j) != 1) throw new Exception("ENTRY WAS NOT ADDED");

            Action a = q.Dequeue();
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