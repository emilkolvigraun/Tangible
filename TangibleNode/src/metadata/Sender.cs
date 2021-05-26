using System;

namespace TangibleNode
{
    public class Sender
    {
        public string Host {get; set;}
        public int Port {get; set;}
        public string ID {get; set;}

        public static Sender Self 
        {
            get 
            {
                return new Sender(){
                    Host = Params.HOST,
                    Port = Params.PORT,
                    ID = Params.ID
                };
            }
        }
    }
}