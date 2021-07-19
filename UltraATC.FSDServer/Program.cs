using System;
using System.Threading.Tasks;

namespace UltraATC.FSDServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new FSDServer();
            await server.Start();
        }
    }
}
