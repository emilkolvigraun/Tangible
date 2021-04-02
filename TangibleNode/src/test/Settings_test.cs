using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TangibleNode
{
    class Connect_test
    {

        public static void Run()
        {
            Console.WriteLine(JsonConvert.SerializeObject(
                new Settings(){
                    Host = "1212",
                    ID = "2323",
                    Port = 1,
                    TcpNodes = new List<Node>{
                        new Node(){
                            ID = "bacon0",
                            Host = "host",
                            Port = 1234
                        },
                        new Node(){
                            ID = "bacon1",
                            Host = "host",
                            Port = 1234
                        }
                    }
                }, Formatting.None));
        }

    }
}