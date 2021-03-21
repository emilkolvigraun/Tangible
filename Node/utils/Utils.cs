using System;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace Node
{
    class Utils
    {
        internal static readonly char[] chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static string GetUniqueKey(int size)
        {
            byte[] data = new byte[4 * size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

        private static Random random = new Random();
        public static int GetRandomInt(int x, int y)
        {
            return random.Next(x, y);
        }

        /// <summary>Class to get current timestamp with enough precision</summary>
        private static readonly DateTime Jan1St1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
        public static long Micros { get { return (long)(DateTime.UtcNow.Ticks / (TimeSpan.TicksPerMillisecond / 1000));}}
        public static void Wait(int time = -1)
        {
            if (time == -1) time = Params.WAIT_TIME_MS;
            long t0 = Utils.Millis;
            while (true)
            {
                if (Utils.Millis - t0 > time) break;
            }
            Logger.Log("Utils", "Waited " + time + "ms", Logger.LogLevel.DEBUG);
        }

        public static double ResourceUsage
        {
            get 
            {
                var proc = Process.GetCurrentProcess();
                return proc.TotalProcessorTime.TotalSeconds;
            }
        }
    }
}