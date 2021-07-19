using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace UltraATC.FSDServer
{
    public class FSDServer
    {
        

        private TcpListener tcpListener;

        //ipaddress, User Class
        public Dictionary<EndPoint, TCPUser> Connections = new Dictionary<EndPoint, TCPUser>();

        public async Task Start()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Gray;

            tcpListener = new TcpListener(IPAddress.Any, 6809);
            tcpListener.Start();

            //Keep Thread Open
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Awaiting Connections...");
                Console.WriteLine("Currently there are " + Connections.Count + " connections.");
                TcpClient Client = await tcpListener.AcceptTcpClientAsync();

                //Accept Client on another thread so that more clients can still connect without blocking
                AcceptClient(Client);

            }
        }

        private async Task AcceptClient(TcpClient client)
        {
            try
            {
                //Check Dictionary to prevent multiple connections from same endpoint.
                if (!Connections.ContainsKey(client.Client.RemoteEndPoint))
                {
                    Connections.Add(client.Client.RemoteEndPoint, new TCPUser(client, this));
                    Connections[client.Client.RemoteEndPoint].Initialize();

                }
                else
                {
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
