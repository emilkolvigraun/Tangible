using System;
using System.Collections.Generic;

namespace Node 
{
    class SerializeDeserialize_test
    {
        public static void Run()
        {
            ActionRequest request = new ActionRequest(){
                TypeOf = RequestType.SUBSCRIBE,
                Priority = 2,
                User = Utils.GetUniqueKey(size: 10),
                Data = new Dictionary<string, string>(){{"benv","..."}},
                ReturnTopic = "MyApplication"
            };

            Console.WriteLine(request.EncodeRequestStr());
            Console.WriteLine("Are they still the same? " + (request.EncodeRequestStr() == request.EncodeRequest().ParseRequest().EncodeRequestStr()?"yes":"no"));
            Console.WriteLine("ReturnTopic: " + ((ActionRequest)request.EncodeRequestStr().ParseRequest()).ReturnTopic + (((ActionRequest)request.EncodeRequestStr().ParseRequest()).ReturnTopic==request.ReturnTopic?" equals":" not equal"));

            Environment.Exit(0);
        }
    }
}