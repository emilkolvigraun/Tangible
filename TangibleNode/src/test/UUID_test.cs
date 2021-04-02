using System;
using System.Collections.Generic;

namespace TangibleNode 
{
    class UUID_test 
    {
        public static void Run()
        {
            // generate 15 million uuids and verify that none are the same
            HashSet<string> uuids = new HashSet<string>();
            for (int i = 0; i<15000000; i++)
            {
                string uuid = Utils.GenerateUUID();
                if (uuids.Contains(uuid)) throw new Exception("UUID NOT UNIQUE");
                uuids.Add(uuid);
            }

            Console.WriteLine("UUID_test successfull");
        }
    }
}