using System.Text;
using System.Net.Sockets;  

namespace TangibleNode
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public static int BufferSize = 2048*Params.BATCH_SIZE;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;

        public int Retries = 0;
    }  
}