using FlightEvents.Client.Logics;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlightEvents.Client.SimConnectFSX
{
    public class SimConnectFlightConnector : IFlightConnector
    {
        public event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        public event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        public event EventHandler AircraftPositionChanged;
        public event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
        public event EventHandler Connected;
        public event EventHandler Closed;

        private TaskCompletionSource<FlightPlanData> flightPlanTcs = null;
        private TaskCompletionSource<AircraftData> aircraftDataTcs = null;


        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;
        private readonly ILogger<SimConnectFlightConnector> logger;

        public IntPtr Handle { get; private set; }

        private SimConnect simconnect = null;
        private CancellationTokenSource cts = null;

        public SimConnectFlightConnector(ILogger<SimConnectFlightConnector> logger)
        {
            this.logger = logger;
        }

        #region Public Methods

        // Simconnect client will send a win32 message when there is 
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.
        public IntPtr HandleSimConnectEvents(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            isHandled = false;

            switch (message)
            {
                case WM_USER_SIMCONNECT:
                    {
                        if (simconnect != null)
                        {
                            try
                            {
                                this.simconnect.ReceiveMessage();
                            }
                            catch (Exception ex)
                            {
                                RecoverFromError(ex);
                            }

                            isHandled = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return IntPtr.Zero;
        }

        // Set up the SimConnect event handlers
        public void Initialize(IntPtr Handle)
        {
            simconnect = new SimConnect("Flight Broadcaster", Handle, WM_USER_SIMCONNECT, null, 0);

            // listen to connect and quit msgs
            simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
            simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);

            // listen to exceptions
            simconnect.OnRecvException += Simconnect_OnRecvException;

            simconnect.OnRecvSimobjectDataBytype += Simconnect_OnRecvSimobjectDataBytypeAsync;
            simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
            RegisterAircraftDataDefinition();
            RegisterFlightStatusDefinition();
            RegisterAircraftPositionDefinition();

            simconnect.SubscribeToSystemEvent(EVENTS.POSITION_CHANGED, "PositionChanged");
            simconnect.OnRecvEvent += Simconnect_OnRecvEvent;

            simconnect.OnRecvSystemState += Simconnect_OnRecvSystemState;

            Connected?.Invoke(this, new EventArgs());
        }

        public void Send(string message)
        {
            simconnect?.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 3, EVENTS.MESSAGE_RECEIVED, message);
        }

        public void Teleport(double latitude, double longitude, double altitude)
        {
            simconnect.SetDataOnSimObject(DEFINITIONS.AircraftPosition, 0, SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new AircraftPositionStruct { Latitude = latitude, Longitude = longitude, Altitude = altitude });
        }

        public void CloseConnection()
        {
            try
            {
                cts?.Cancel();
                cts = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cannot cancel request loop! Error: {ex.Message}");
            }
            try
            {
                if (simconnect != null)
                {
                    // Dispose serves the same purpose as SimConnect_Close()
                    simconnect.Dispose();
                    simconnect = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cannot unsubscribe events! Error: {ex.Message}");
            }
        }

        public Task<FlightPlanData> RequestFlightPlanAsync(CancellationToken cancellationToken = default)
        {
            if (flightPlanTcs != null)
            {
                logger.LogInformation("Wait for existing flight plan request...");
                return flightPlanTcs.Task;
            }

            var tcs = new TaskCompletionSource<FlightPlanData>();
            flightPlanTcs = tcs;

            cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    logger.LogWarning("Cannot get flight plan in time limit!");
                    flightPlanTcs = null;
                }
            }, useSynchronizationContext: false);

            logger.LogInformation("Requesting flight plan...");
            simconnect.RequestSystemState(DATA_REQUESTS.FLIGHT_PLAN, "FlightPlan");

            return flightPlanTcs.Task;
        }

        public Task<AircraftData> RequestAircraftDataAsync(CancellationToken cancellationToken = default)
        {
            if (aircraftDataTcs != null)
            {
                logger.LogInformation("Wait for existing aircraft data request...");
                return aircraftDataTcs.Task;
            }

            var tcs = new TaskCompletionSource<AircraftData>();
            aircraftDataTcs = tcs;

            cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    logger.LogWarning("Cannot get aircraft data in time limit!");
                    aircraftDataTcs = null;
                }
            }, useSynchronizationContext: false);

            logger.LogInformation("Requesting aircraft data...");
            simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.AIRCRAFT_DATA, DEFINITIONS.AircraftData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

            return aircraftDataTcs.Task;
        }

        #endregion

        #region Private Methods

        #region Register Data Definitions

        private void RegisterAircraftDataDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.AircraftData,
                "ATC TYPE",
                null,
                SIMCONNECT_DATATYPE.STRING32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.AircraftData,
                "ATC MODEL",
                null,
                SIMCONNECT_DATATYPE.STRING32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.AircraftData,
                "Title",
                null,
                SIMCONNECT_DATATYPE.STRING256,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.AircraftData,
                "ESTIMATED CRUISE SPEED",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<AircraftDataStruct>(DEFINITIONS.AircraftData);
        }

        private void RegisterFlightStatusDefinition()
        {
            //simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
            //    "SIM TIME",
            //    "Seconds",
            //    SIMCONNECT_DATATYPE.FLOAT32,
            //    0.0f,
            //    SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIMULATION RATE",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LATITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LONGITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALTITUDE",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALT ABOVE GROUND",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE PITCH DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE BANK DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES TRUE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES MAGNETIC",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND ALTITUDE",
                "Meters",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND VELOCITY",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AIRSPEED INDICATED",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "VERTICAL SPEED",
                "Feet per minute",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "FUEL TOTAL QUANTITY",
                "Gallons",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND VELOCITY",
                "Feet per second",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND DIRECTION",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIM ON GROUND",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "STALL WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "OVERSPEED WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT MASTER",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "TRANSPONDER CODE:1",
                "Hz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "COM ACTIVE FREQUENCY:1",
                "kHz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "COM ACTIVE FREQUENCY:2",
                "kHz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<FlightStatusStruct>(DEFINITIONS.FlightStatus);
        }

        private void RegisterAircraftPositionDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.AircraftPosition,
                "PLANE LATITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.AircraftPosition,
                "PLANE LONGITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.AircraftPosition,
                "PLANE ALTITUDE",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<FlightStatusStruct>(DEFINITIONS.AircraftPosition);
        }

        #endregion

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            // Must be general SimObject information
            switch (data.dwRequestID)
            {
                case (uint)DATA_REQUESTS.FLIGHT_STATUS:
                    {
                        var flightStatus = data.dwData[0] as FlightStatusStruct?;

                        if (flightStatus.HasValue)
                        {
                            logger.LogTrace("Get Aircraft status");
                            AircraftStatusUpdated?.Invoke(this, new AircraftStatusUpdatedEventArgs(
                                new AircraftStatus
                                {
                                    //SimTime = flightStatus.Value.SimTime,
                                    //SimRate = flightStatus.Value.SimRate,
                                    Latitude = flightStatus.Value.Latitude,
                                    Longitude = flightStatus.Value.Longitude,
                                    Altitude = flightStatus.Value.Altitude,
                                    AltitudeAboveGround = flightStatus.Value.AltitudeAboveGround,
                                    Pitch = flightStatus.Value.Pitch,
                                    Bank = flightStatus.Value.Bank,
                                    Heading = flightStatus.Value.MagneticHeading,
                                    TrueHeading = flightStatus.Value.TrueHeading,
                                    GroundSpeed = flightStatus.Value.GroundSpeed,
                                    IndicatedAirSpeed = flightStatus.Value.IndicatedAirSpeed,
                                    VerticalSpeed = flightStatus.Value.VerticalSpeed,
                                    FuelTotalQuantity = flightStatus.Value.FuelTotalQuantity,
                                    IsOnGround = flightStatus.Value.IsOnGround == 1,
                                    StallWarning = flightStatus.Value.StallWarning == 1,
                                    OverspeedWarning = flightStatus.Value.OverspeedWarning == 1,
                                    IsAutopilotOn = flightStatus.Value.IsAutopilotOn == 1,
                                    Transponder = flightStatus.Value.Transponder.ToString().PadLeft(4, '0'),
                                    FrequencyCom1 = flightStatus.Value.Com1,
                                    FrequencyCom2 = flightStatus.Value.Com2,
                                }));
                        }
                    }
                    break;
            }
        }

        private void Simconnect_OnRecvSimobjectDataBytypeAsync(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            // Must be general SimObject information
            switch (data.dwRequestID)
            {
                case (uint)DATA_REQUESTS.AIRCRAFT_DATA:
                    {
                        // Handle this when aircraft is changed
                        var aircraftData = data.dwData[0] as AircraftDataStruct?;

                        if (aircraftData.HasValue)
                        {
                            logger.LogInformation("Aircraft data received.");

                            var result = new AircraftData
                            {
                                Type = aircraftData.Value.Type,
                                Model = aircraftData.Value.Model,
                                Title = aircraftData.Value.Title,
                                EstimatedCruiseSpeed = aircraftData.Value.EstimatedCruiseSpeed
                            };
                            AircraftDataUpdated?.Invoke(this, new AircraftDataUpdatedEventArgs(result));

                            aircraftDataTcs?.TrySetResult(result);
                            aircraftDataTcs = null;
                        }
                    }
                    break;
            }
        }

        void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            logger.LogInformation("OnRecvEvent dwID " + data.dwID + " uEventID " + data.uEventID);
            switch ((SIMCONNECT_RECV_ID)data.dwID)
            {
                case SIMCONNECT_RECV_ID.EVENT_FILENAME:

                    break;
                case SIMCONNECT_RECV_ID.QUIT:
                    logger.LogInformation("Quit");
                    break;
            }

            switch ((EVENTS)data.uEventID)
            {
                case EVENTS.POSITION_CHANGED:
                    logger.LogInformation("Position changed");
                    AircraftPositionChanged?.Invoke(this, new EventArgs());
                    break;
            }
        }

        private async void Simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            switch (data.dwRequestID)
            {
                case (int)DATA_REQUESTS.FLIGHT_PLAN:
                    if (!string.IsNullOrEmpty(data.szString))
                    {
                        logger.LogInformation($"Received flight plan {data.szString}");

                        var planName = data.szString;

                        if (planName == ".PLN")
                        {
                            logger.LogInformation("Flight plan is not read. Wait for 5s...");
                            await Task.Delay(5000);

                            simconnect.RequestSystemState(DATA_REQUESTS.FLIGHT_PLAN, "FlightPlan");
                        }
                        else
                        {
                            if (File.Exists(planName))
                            {
                                using var stream = File.OpenRead(planName);
                                var serializer = new XmlSerializer(typeof(FlightPlanDocumentXml));
                                var flightPlan = serializer.Deserialize(stream) as FlightPlanDocumentXml;

                                var flightPlanData = flightPlan.FlightPlan.ToData();
                                FlightPlanUpdated?.Invoke(this, new FlightPlanUpdatedEventArgs(flightPlanData));

                                flightPlanTcs?.TrySetResult(flightPlanData);
                                flightPlanTcs = null;
                            }
                            else
                            {
                                logger.LogWarning($"{planName} does not exist!");

                                FlightPlanUpdated?.Invoke(this, new FlightPlanUpdatedEventArgs(null));
                                flightPlanTcs?.TrySetResult(null);
                                flightPlanTcs = null;
                            }
                        }
                    }
                    break;
            }
        }

        void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger.LogInformation("Connected to Flight Simulator");

            simconnect.RequestDataOnSimObject(DATA_REQUESTS.FLIGHT_STATUS, DEFINITIONS.FlightStatus, 0, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        // The case where the user closes Flight Simulator
        void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            logger.LogInformation("Flight Simulator has exited");
            Closed?.Invoke(this, new EventArgs());
            CloseConnection();
        }

        void Simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            logger.LogError("Exception received: {error}", (SIMCONNECT_EXCEPTION)data.dwException);
        }

        private void RecoverFromError(Exception exception)
        {
            // 0xC000014B: CTD
            // 0xC00000B0: Sim has exited
            logger.LogError(exception, "Exception received");
            CloseConnection();
            Closed?.Invoke(this, new EventArgs());
        }

        #endregion
    }
}
