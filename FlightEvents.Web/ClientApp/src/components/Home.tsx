import * as React from 'react';
import * as signalr from '@microsoft/signalr';
import 'msgpack5';
import * as protocol from '@microsoft/signalr-protocol-msgpack';
import { convertPropertyNames, pascalCaseToCamelCase } from '../Converters';
import { AircraftStatus, Airport, FlightPlan, FlightPlanData, ATCStatus, ATCInfo, AircraftStatusBrief } from '../Models';
import AircraftList, { AircraftStatusInList } from './AircraftList';
import ControllerList from './ControllerList';
import EventList from './EventList';
import Display from './Display';
import Hud from './Hud';
import { IMap, MapTileType, View, MapPosition } from '../maps/IMap';
import LeafletMap from '../maps/LeaftletMap';
import MaptalksMap from '../maps/MaptalksMap';
import Storage from '../Storage';
import { deepEqual } from '../Compare';
import TeleportDialog from './Dialogs/TeleportDialog';
import { RouteComponentProps } from 'react-router-dom';

const CONTROLLER_TIMEOUT_MILLISECONDS = 30000;
const AIRCRAFT_TIMEOUT_MILLISECONDS = 10000;

interface State {
    controllers: { [clientId: string]: ATCInfo & ATCStatus };

    aircrafts: { [clientId: string]: AircraftStatusInList };
    aircraftCallsigns: { [clientId: string]: string };
    myClientId: string | null;
    showPathClientIds: string[];
    followingClientId: string | null;
    moreInfoClientIds: string[];
    flightPlanClientId: string | null;

    isDark: boolean;
    map3D: boolean;
    mapTileType: MapTileType;

    movingPosition: MapPosition | null;
}

type Props = RouteComponentProps<any>;

export class Home extends React.Component<Props, State> {
    static displayName = Home.name;

    private storage = new Storage();

    private hub: signalr.HubConnection;
    private map: IMap;
    private currentView?: View;

    private myCallsign: string | null = null;
    private followCallsign: string | null = null;
    private focusCallsign: string | null = null;

    private aircrafts: { [clientId: string]: { lastUpdated: Date, status: AircraftStatus } } = {};
    private controllers: { [clientId: string]: { lastUpdated: Date } } = {};

    constructor(props: Props) {
        super(props);

        const pref = this.storage.loadPreferences();

        this.state = {
            controllers: {},
            aircrafts: {},
            aircraftCallsigns: {},
            myClientId: null,
            showPathClientIds: [],
            followingClientId: null,
            moreInfoClientIds: [],
            flightPlanClientId: null,
            isDark: pref ? pref.isDark : false,
            map3D: pref ? pref.map3D : false,
            mapTileType: pref ? pref.mapTileType : MapTileType.OpenStreetMap,
            movingPosition: null
        }

        this.map = !this.state.map3D ? new LeafletMap() : new MaptalksMap();

        this.hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub?clientType=Web')
            .withAutomaticReconnect()
            .withHubProtocol(new protocol.MessagePackHubProtocol())
            .build();

        this.handleControllerClick = this.handleControllerClick.bind(this);
        this.handleAircraftClick = this.handleAircraftClick.bind(this);

        this.handleIsDarkChanged = this.handleIsDarkChanged.bind(this);
        this.handleMapDimensionChanged = this.handleMapDimensionChanged.bind(this);
        this.handleTileTypeChanged = this.handleTileTypeChanged.bind(this);

        this.handleMeChanged = this.handleMeChanged.bind(this);
        this.handleShowPathChanged = this.handleShowPathChanged.bind(this);
        this.handleFollowingChanged = this.handleFollowingChanged.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
        this.handleFlightPlanChanged = this.handleFlightPlanChanged.bind(this);

        this.handleAirportsLoaded = this.handleAirportsLoaded.bind(this);
        this.handleFlightPlansLoaded = this.handleFlightPlansLoaded.bind(this);
        this.handleTeleportCompleted = this.handleTeleportCompleted.bind(this);

        this.cleanUp = this.cleanUp.bind(this);
    }

    shouldComponentUpdate(nextProps, nextState) {
        return !deepEqual(this.state, nextState) || this.props !== nextProps;
    }

    async componentDidMount() {
        this.initializeMap();

        const searchParams = new URLSearchParams(this.props.location.search);
        this.myCallsign = searchParams.get('myCallsign');
        this.followCallsign = searchParams.get('followCallsign');
        this.focusCallsign = searchParams.get('focusCallsign');

        const hub = this.hub;

        hub.onreconnected(async connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);

            await hub.send('Join', 'Map');
        })

        hub.on("UpdateATC", (clientId, status: ATCStatus, atc: ATCInfo) => {
            status = convertPropertyNames(status, pascalCaseToCamelCase);
            atc = convertPropertyNames(atc, pascalCaseToCamelCase);

            try {
                if (atc && status) {
                    this.setState({
                        controllers: {
                            ...this.state.controllers,
                            [clientId]: {
                                ...atc,
                                ...status
                            }
                        }
                    });

                    this.controllers[clientId] = {
                        lastUpdated: new Date()
                    };
                } else {
                    // Remove
                    this.cleanUpController(clientId);
                }
                this.map.moveATCMarker(clientId, status, atc);
            } catch (e) {
                console.error(e);
            }
        });

        hub.on("UpdateAircraft", (clientId, aircraftStatus: AircraftStatus) => {
            aircraftStatus = convertPropertyNames(aircraftStatus, pascalCaseToCamelCase);

            try {
                const isReady = !(Math.abs(aircraftStatus.latitude) < 0.02 && Math.abs(aircraftStatus.longitude) < 0.02);

                let newState = {
                    ...this.state,
                    aircrafts: {
                        ...this.state.aircrafts,
                        [clientId]: {
                            callsign: aircraftStatus.callsign,
                            isReady: isReady
                        }
                    },
                    aircraftCallsigns: {
                        ...this.state.aircraftCallsigns,
                        [clientId]: aircraftStatus.callsign
                    }
                };

                // Set focus aircraft from URL
                if (this.focusCallsign && aircraftStatus.callsign === this.focusCallsign) {
                    if (!this.state.followingClientId) {
                        // Only when not following
                        this.map.focus(aircraftStatus);
                    }
                    this.focusCallsign = null;
                }
                // Set follow aircraft from URL
                if (this.followCallsign && aircraftStatus.callsign === this.followCallsign) {
                    newState = {
                        ...newState,
                        followingClientId: clientId
                    };
                    this.followCallsign = null;
                }
                // Set own aircraft from URL
                if (this.myCallsign && aircraftStatus.callsign === this.myCallsign) {
                    newState = {
                        ...newState,
                        myClientId: clientId
                    };
                    if (!this.state.followingClientId) {
                        this.map.focus(aircraftStatus);
                    }
                    this.myCallsign = null;
                }

                this.setState(newState);

                this.aircrafts[clientId] = {
                    lastUpdated: new Date(),
                    status: aircraftStatus
                };

                if (isReady) {
                    this.map.moveMarker(clientId, aircraftStatus,
                        this.state.myClientId === clientId,
                        this.state.followingClientId === clientId,
                        this.state.flightPlanClientId === clientId,
                        this.state.moreInfoClientIds.includes(clientId),
                        this.state.showPathClientIds.includes(clientId));

                    if (this.state.showPathClientIds.includes(clientId)) {
                        this.map.track(clientId, aircraftStatus);
                    }
                } else {
                    // Aircraft not loaded
                    if (this.state.showPathClientIds.includes(clientId)) {
                        this.map.clearTrack(clientId);
                    }
                    this.map.cleanUpAircraft(clientId, this.state.myClientId === clientId);
                }
            } catch (e) {
                console.error(e);
            }
        });

        hub.on("ReturnFlightPlanDetails", (connectionId, flightPlan: FlightPlanData | null) => {
            if (flightPlan) {
                flightPlan = convertPropertyNames(flightPlan, pascalCaseToCamelCase) as FlightPlanData;
                this.map.drawFlightPlans([flightPlan]);
            }
        });

        await hub.start();

        setInterval(this.cleanUp, 2000);
    }

    private initializeMap() {
        this.map.onViewChanged(view => {
            this.currentView = view;
        });
        this.map.onAircraftMoved(position => {
            this.setState({ movingPosition: position })
        });
        this.map.onSetMe(clientId => {
            this.handleMeChanged(clientId);
        });
        this.map.onSetFollow(clientId => {
            this.handleFollowingChanged(clientId);
        });
        this.map.onSetShowPlan(clientId => {
            this.handleFlightPlanChanged(clientId);
        });
        this.map.onSetShowInfo(clientId => {
            this.handleMoreInfoChanged(clientId);
        });
        this.map.onSetShowRoute(clientId => {
            this.handleShowPathChanged(clientId);
        });
        this.map.initialize('mapid', this.currentView);
        this.map.setTileLayer(this.state.mapTileType);
        this.map.changeMode(this.state.isDark);
    }

    private cleanUp() {
        for (let clientId of Object.keys(this.controllers)) {
            const controller = this.controllers[clientId];
            if (new Date().getTime() - controller.lastUpdated.getTime() > CONTROLLER_TIMEOUT_MILLISECONDS) {
                this.cleanUpController(clientId);
            }
        }

        for (let clientId of Object.keys(this.aircrafts)) {
            const aircraft = this.aircrafts[clientId];
            if (new Date().getTime() - aircraft.lastUpdated.getTime() > AIRCRAFT_TIMEOUT_MILLISECONDS) {
                this.cleanUpAircraft(clientId);
            }
        }
    }

    private cleanUpController(clientId: string) {
        this.map.cleanUpController(clientId);

        let newControllers = {
            ...this.state.controllers
        };
        delete newControllers[clientId];
        this.setState({
            controllers: newControllers
        });

        delete this.controllers[clientId];
    }

    private cleanUpAircraft(clientId: string) {
        this.map.cleanUpAircraft(clientId, clientId === this.state.myClientId);

        if (clientId === this.state.myClientId) {
            this.setState({
                myClientId: null
            });
        }
        if (clientId === this.state.followingClientId) {
            this.setState({
                followingClientId: null
            });
        }
        let newAircrafts = {
            ...this.state.aircrafts
        };
        let newAircraftCallsigns = {
            ...this.state.aircraftCallsigns
        }
        delete newAircrafts[clientId];
        delete newAircraftCallsigns[clientId];
        this.setState({
            aircrafts: newAircrafts,
            aircraftCallsigns: newAircraftCallsigns
        })

        delete this.aircrafts[clientId];
    }

    private handleControllerClick(clientId: string) {
        if (this.map) {
            this.map.focus(this.state.controllers[clientId]);
        }
    }

    private handleAircraftClick(clientId: string) {
        if (this.map) {
            this.map.focus(this.aircrafts[clientId].status);
        }
    }

    private savePreferences() {
        this.storage.savePreferences({
            isDark: this.state.isDark,
            map3D: this.state.map3D,
            mapTileType: this.state.mapTileType
        });
    }

    private handleIsDarkChanged(isDark: boolean) {
        this.setState({ isDark: isDark }, () => {
            this.map.changeMode(this.state.isDark);
            this.savePreferences();
        });
    }

    private handleMapDimensionChanged(dimension: "2D" | "3D") {
        this.setState({ map3D: dimension === "3D" }, () => {
            this.savePreferences();
        });

        this.map?.deinitialize();
        this.map = dimension === "2D" ? new LeafletMap() : new MaptalksMap();
        this.initializeMap();
    }

    private handleTileTypeChanged(tileType: MapTileType) {
        this.setState({ mapTileType: tileType }, () => {
            this.savePreferences();
        });
        this.map.setTileLayer(tileType);
    }

    private handleMeChanged(clientId: string | null) {
        this.setState({ myClientId: clientId });

        if (clientId) {
            this.map.addRangeCircle();
        } else {
            this.map.removeRangeCircle();
        }
    }

    private handleShowPathChanged(clientId: string) {
        if (this.state.showPathClientIds.includes(clientId)) {
            // Remove
            this.setState({ showPathClientIds: this.state.showPathClientIds.filter(o => o !== clientId) });
            this.map.clearTrack(clientId);
        } else {
            // Add
            this.setState({ showPathClientIds: this.state.showPathClientIds.concat(clientId) });
            this.map.clearTrack(clientId);
            let route: AircraftStatusBrief[] = [];
            this.hub.stream("RequestFlightRoute", clientId)
                .subscribe({
                    next: item => {
                        route = [item].concat(route);
                    },
                    complete: () => {
                        route = convertPropertyNames(route, pascalCaseToCamelCase) as AircraftStatusBrief[];
                        this.map.prependTrack(clientId, route);
                    },
                    error: () => {

                    }
                });
        }
    }

    private handleFollowingChanged(clientId: string | null) {
        this.setState({ followingClientId: clientId });
    }

    private handleMoreInfoChanged(clientId: string) {
        if (this.state.moreInfoClientIds.includes(clientId)) {
            this.setState({ moreInfoClientIds: this.state.moreInfoClientIds.filter(o => o !== clientId) });
        } else {
            this.setState({ moreInfoClientIds: this.state.moreInfoClientIds.concat(clientId) });
        }
    }

    private handleFlightPlanChanged(clientId: string | null) {
        this.setState({ flightPlanClientId: clientId }, () => {
            if (clientId == null) {
                // Clear map
                this.map.drawFlightPlans([]);
            } else {
                // Request plan
                this.hub.send('RequestFlightPlanDetails', clientId);
            }
        });
    }

    public handleAirportsLoaded(airports: Airport[]) {
        this.map.drawAirports(airports);
    }

    public handleFlightPlansLoaded(flightPlans: FlightPlan[]) {
        this.map.drawFlightPlans(flightPlans.map(o => o.data));
    }

    public handleTeleportCompleted() {
        this.setState({ movingPosition: null });
    }

    render() {
        return <>
            {this.state.isDark && <style dangerouslySetInnerHTML={{ __html: `.leaflet-container { background-color: black } .leaflet-tile, .icon-aircraft-marker { -webkit-filter: hue-rotate(180deg) invert(100%); }` }} />}
            <div id="mapid" style={{ height: '100%' }}></div>

            <Display
                isDark={this.state.isDark} onIsDarkChanged={this.handleIsDarkChanged}
                dimension={this.state.map3D ? "3D" : "2D"} onDimensionChanged={this.handleMapDimensionChanged}
                tileType={this.state.mapTileType} onTileTypeChanged={this.handleTileTypeChanged} />

            <Hud
                aircrafts={this.state.aircraftCallsigns} onAircraftClick={this.handleAircraftClick}
                onMeChanged={this.handleMeChanged} myClientId={this.state.myClientId}
                onFollowingChanged={this.handleFollowingChanged} followingClientId={this.state.followingClientId}
                onFlightPlanChanged={this.handleFlightPlanChanged} flightPlanClientId={this.state.flightPlanClientId}
            />

            <ControllerList controllers={this.state.controllers} onControllerClick={this.handleControllerClick} />

            <AircraftList
                aircrafts={this.state.aircrafts} onAircraftClick={this.handleAircraftClick}
                myClientId={this.state.myClientId}
                followingClientId={this.state.followingClientId}
                flightPlanClientId={this.state.flightPlanClientId}
                onShowPathChanged={this.handleShowPathChanged} showPathClientIds={this.state.showPathClientIds}
                onMoreInfoChanged={this.handleMoreInfoChanged} moreInfoClientIds={this.state.moreInfoClientIds}
            />

            <EventList hub={this.hub} onAirportsLoaded={this.handleAirportsLoaded} onFlightPlansLoaded={this.handleFlightPlansLoaded} />

            <TeleportDialog hub={this.hub} selectedPosition={this.state.movingPosition} onComplete={this.handleTeleportCompleted} />
        </>;
    }
}