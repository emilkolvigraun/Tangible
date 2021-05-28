using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TangibleNode
{
    class Settings_test
    {

        public static void Run()
        {
            Console.WriteLine(JsonConvert.SerializeObject(
                new Settings(){
                    Host = "1212",
                    ID = "2323",
                    Port = 1,
                    Members = new List<Credentials>{
                        new Credentials(){
                            ID = "bacon0",
                            Host = "host",
                            Port = 1234
                        },
                        new Credentials(){
                            ID = "bacon1",
                            Host = "host",
                            Port = 1234
                        }
                    }
                }, Formatting.None));
            Environment.Exit(0);
        }

    }
}