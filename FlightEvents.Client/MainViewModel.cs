using FlightEvents.Client.Logics;
using FlightEvents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlightEvents.Client
{
    public enum ConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Failed
    }

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if ((storage == null && value != null) || (storage != null && !storage.Equals(value)))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly ILogger<MainViewModel> logger;
        private readonly IEventGraphQLClient graphQLClient;
        private readonly object webServerUrl;

        public MainViewModel(ILogger<MainViewModel> logger, IEventGraphQLClient graphQLClient, IOptionsMonitor<AppSettings> appSettings)
        {
            this.logger = logger;
            this.graphQLClient = graphQLClient;
            this.webServerUrl = appSettings.CurrentValue.WebServerUrl;
        }

        public async Task InitializeAsync(string clientId)
        {
            try
            {
                var allEvents = await graphQLClient.GetFlightEventsAsync();
                Events = new ObservableCollection<FlightEvent>(allEvents);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot get events!");
            }

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{webServerUrl}/Discord/Connection/{clientId}");

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var connection = await JsonSerializer.DeserializeAsync<DiscordConnection>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    DiscordConnection = connection;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot get Discord connection!");
            }
        }

        private ObservableCollection<FlightEvent> events = new ObservableCollection<FlightEvent>();
        public ObservableCollection<FlightEvent> Events { get => events; set => SetProperty(ref events, value); }

        private ConnectionState simConnectionState = ConnectionState.Idle;
        public ConnectionState SimConnectionState { get => simConnectionState; set => SetProperty(ref simConnectionState, value); }

        private ConnectionState hubConnectionState = ConnectionState.Idle;
        public ConnectionState HubConnectionState { get => hubConnectionState; set => SetProperty(ref hubConnectionState, value); }

        private ConnectionState atcConnectionState = ConnectionState.Idle;
        public ConnectionState AtcConnectionState { get => atcConnectionState; set => SetProperty(ref atcConnectionState, value); }

        private string callsign = null;
        public string Callsign { get => callsign; set => SetProperty(ref callsign, value?.Replace("<", "").Replace(">", "")); }

        private AircraftStatus aircraftStatus = null;
        public AircraftStatus AircraftStatus { get => aircraftStatus; set => SetProperty(ref aircraftStatus, value); }

        private bool transponderIdent;
        public bool TransponderIdent { get => transponderIdent; set => SetProperty(ref transponderIdent, value); }

        private string remarks;
        public string Remarks { get => remarks; set => SetProperty(ref remarks, value); }

        private bool vatsimMode = false;
        public bool VatsimMode { get => vatsimMode; set => SetProperty(ref vatsimMode, value); }

        private string atcCallsign = null;
        public string AtcCallsign { get => atcCallsign; set => SetProperty(ref atcCallsign, value); }

        private bool isTracking;
        public bool IsTracking { get => isTracking; set => SetProperty(ref isTracking, value); }

        private DiscordConnection discordConnection;
        public DiscordConnection DiscordConnection
        {
            get => discordConnection;
            set
            {
                if (discordConnection != value && ChecklistEvent != null)
                {
                    var itemVm = ChecklistEvent?.Items.FirstOrDefault(o => o.Data.Type == FlightEventChecklistItemType.Client && o.Data.SubType == FlightEventChecklistItemSubType.ConnectToDiscord);
                    if (itemVm != null)
                    {
                        var oldEnabled = itemVm.IsEnabled;
                        itemVm.IsEnabled = true;
                        itemVm.IsChecked = value != null;
                        itemVm.IsEnabled = oldEnabled;
                    }
                }
                SetProperty(ref discordConnection, value);
            }
        }

        private bool disableDiscordRP;
        public bool DisableDiscordRP { get => disableDiscordRP; set => SetProperty(ref disableDiscordRP, value); }

        private bool broadcastUDP;
        public bool BroadcastUDP { get => broadcastUDP; set => SetProperty(ref broadcastUDP, value); }

        private string broadcastIP = null;
        public string BroadcastIP { get => broadcastIP; set => SetProperty(ref broadcastIP, value); }

        private bool slowMode;
        public bool SlowMode { get => slowMode; set => SetProperty(ref slowMode, value); }

        private bool minimizeToTaskbar;
        public bool MinimizeToTaskbar { get => minimizeToTaskbar; set => SetProperty(ref minimizeToTaskbar, value); }

        private Airport nearestAirport;
        public Airport NearestAirport { get => nearestAirport; set => SetProperty(ref nearestAirport, value); }

        private ChecklistViewModel checklistEvent;
        public ChecklistViewModel ChecklistEvent { get => checklistEvent; set => SetProperty(ref checklistEvent, value); }
    }
}