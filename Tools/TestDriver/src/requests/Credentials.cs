using System;

namespace TangibleDriver
{
    public class Credentials
    {
        public string Host {get; set;}
        public int Port {get; set;}
        public string ID {get; set;}

        public static Credentials Self 
        {
            get 
            {
                return new Credentials(){
                    Host = Params.HOST,
                    Port = Params.PORT,
                    ID = Params.ID
                };
            }
        }
    }
}