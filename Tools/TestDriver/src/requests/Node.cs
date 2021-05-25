using System;

namespace TangibleDriver
{
    public class Node
    {
        public string Host {get; set;}
        public int Port {get; set;}
        public string ID {get; set;}

        public static Node Self 
        {
            get 
            {
                return new Node(){
                    Host = Params.HOST,
                    Port = Params.PORT,
                    ID = Params.ID
                };
            }
        }
    }
}