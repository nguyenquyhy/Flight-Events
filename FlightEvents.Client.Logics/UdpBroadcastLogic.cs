using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class UdpBroadcastLogic
    {
        private const int DefaultUdpPort = 49002;
        private const double KnotsToMetersPerSecond = 0.51444444444;
        private const double FeetToMeters = 0.3048;

        private readonly ILogger<UdpBroadcastLogic> logger;
        private UdpClient udpClient = null;
        //private readonly UdpClient udpProbeClient = new UdpClient(new IPEndPoint(IPAddress.Any, 63093));

        private readonly ThrottleExecutor gpsDataSender = new ThrottleExecutor(TimeSpan.FromSeconds(1));
        private readonly ThrottleExecutor attDataSender = new ThrottleExecutor(TimeSpan.FromSeconds(0.5));

        private int currentIcao = 1;
        private readonly Dictionary<string, int> callsignToICAO = new Dictionary<string, int>();

        public UdpBroadcastLogic(ILogger<UdpBroadcastLogic> logger)
        {
            this.logger = logger;
        }

        public async Task StartAsync(string ipAddress)
        {
            try
            {
                if (IPAddress.TryParse(ipAddress, out var ip))
                {
                    udpClient = new UdpClient();
                    udpClient.Connect(new IPEndPoint(ip, DefaultUdpPort));
                }
                else
                {
                    udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, DefaultUdpPort)) { EnableBroadcast = true };
                    udpClient.Connect(new IPEndPoint(IPAddress.Broadcast, DefaultUdpPort));
                }
                //var result = await udpProbeClient.ReceiveAsync();
                //udpClient.Connect(new IPEndPoint(result.RemoteEndPoint.Address, DefaultUdpPort));

            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot establish a UDP connection!");
                //MessageBox.Show($"Cannot estitablish a UDP connection on port {DefaultUdpPort}!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task StopAsync()
        {
            udpClient?.Close();
            udpClient = null;
        }

        private readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public async Task SendTrafficAsync(AircraftStatus status)
        {
            int icaoAddress;
            try
            {
                await sm.WaitAsync();
                if (callsignToICAO.TryGetValue(status.Callsign, out var icao))
                {
                    icaoAddress = icao;
                }
                else
                {
                    icaoAddress = currentIcao++;
                    callsignToICAO.TryAdd(status.Callsign, currentIcao);
                }
            }
            finally
            {
                sm.Release();
            }

            if (udpClient != null)
            {
                var dataString = FormattableString.Invariant($"XTRAFFICFlight Events,{icaoAddress},{status.Latitude:0.######},{status.Longitude:0.######},{status.Altitude:0.#},{status.VerticalSpeed:0.#},{(status.IsOnGround ? 0 : 1)},{status.TrueHeading:0.#},{status.GroundSpeed:0.#},{status.Callsign}");
                var trafficData = Encoding.UTF8.GetBytes(dataString);
                var sent = await udpClient.SendAsync(trafficData, trafficData.Length);
                logger.LogDebug($"Sent {sent}/{trafficData.Length} bytes for Traffic.");
                logger.LogTrace(dataString);
            }
        }

        public async Task SendGpsAsync(AircraftStatus status)
        {
            if (udpClient != null)
            {
                await gpsDataSender.ExecuteAsync(async () =>
                {
                    var gpsData = Encoding.UTF8.GetBytes(FormattableString.Invariant($"XGPSFlight Events,{status.Longitude:0.######},{status.Latitude:0.######},{status.Altitude * FeetToMeters:0.#)},{status.TrueHeading:0.#},{status.GroundSpeed * KnotsToMetersPerSecond:0.#}"));
                    var sent = await udpClient.SendAsync(gpsData, gpsData.Length);
                    logger.LogDebug($"Sent {sent}/{gpsData.Length} bytes for GPS.");
                });
            }
        }

        public async Task SendAttitudeAsync(AircraftStatus status)
        {
            if (udpClient != null)
            {
                await attDataSender.ExecuteAsync(async () =>
                {
                    var statusData = Encoding.UTF8.GetBytes(FormattableString.Invariant($"XATTFlight Events,{status.TrueHeading:0.#},{-status.Pitch:0.#},{-status.Bank:0.#}"));
                    var sent = await udpClient.SendAsync(statusData, statusData.Length);
                    logger.LogDebug($"Sent {sent}/{statusData.Length} bytes for ATT.");
                });
            }
        }
    }
}
