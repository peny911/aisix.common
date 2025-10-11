using System;
using System.Diagnostics;
using System.Text;

namespace Aisix.Common.Utils
{
    public class CuidGenerator
    {
        private static int _counter = 0;
        private static readonly string Fingerprint;
        private const int RandomPartLength = 2;

        static CuidGenerator()
        {
            // Create a fingerprint based on PID and Machine name
            Fingerprint = (Process.GetCurrentProcess().Id + Environment.MachineName).GetHashCode().ToString("X");
        }

        internal static string Generate()
        {
            StringBuilder cuid = new StringBuilder();
            cuid.Append("c");

            // Timestamp
            cuid.Append(DateTime.Now.Ticks.ToString("X"));

            // Counter
            lock (typeof(CuidGenerator))
            {
                if (_counter >= int.MaxValue)
                    _counter = 0;
                cuid.Append(_counter++.ToString("X"));
            }

            // Fingerprint
            cuid.Append(Fingerprint);

            // Random part
            var rnd = Random.Shared;
            for (int i = 0; i < RandomPartLength; i++)
            {
                cuid.Append(rnd.Next(0, 16).ToString("X")); // 0 to 15 in hex
            }

            return cuid.ToString();
        }
    }
}