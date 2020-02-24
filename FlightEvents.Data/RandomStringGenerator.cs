using System;
using System.Text;

namespace FlightEvents.Data
{
    public class RandomStringGenerator
    {
        public static Random random = new Random();

        public string Generate(int length)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)((byte)'a' + random.Next(26)));
            }
            return builder.ToString();
        }
    }
}
