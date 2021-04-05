using System;

namespace TestReceiver
{
    class Utils 
    {
        /// <summary>Generates a universally unique identifier</summary>
        public static string GenerateUUID()
        {
            // chars only
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");//string.Join<char>("", (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")).Where((ch, index) => (index & 1) == 0));
        }

        /// <summary>Class to get current timestamp with enough precision</summary>
        private static readonly DateTime Jan1St1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Get current timestamp in milliseconds</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }

        private static Random random = new Random();

        /// <summary>Returns a random integer in the range [x,y)</summary>
        public static int GetRandomInt(int x, int y)
        {
            return random.Next(x, y);
        }

        
        /// <summary>Returns a random integer in the range [x,y)</summary>
        public static void Sleep(int milliseconds)
        {
            long t0 = Millis;
            while (true)
            {
                if (Millis >= t0+milliseconds) break;
            }
        }
    }   
}