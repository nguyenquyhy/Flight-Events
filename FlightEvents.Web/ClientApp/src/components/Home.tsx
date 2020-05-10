import * as React from 'react';
import styled from 'styled-components';
import { ButtonGroup, Button } from 'reactstrap';
import * as signalr from '@microsoft/signalr';
import 'msgpack5';
//import * as protocol from '@microsoft/signalr-protocol-msgpack';
import { AircraftStatus, Airport, FlightPlan, FlightPlanData, ATCStatus, ATCInfo, AircraftStatusBrief } from '../Models';
import AircraftList from './AircraftList';
import EventList from './EventList';
import { IMap, MapTileType, View } from '../maps/IMap';
import LeafletMap from '../maps/LeaftletMap';
import MaptalksMap from '../maps/MaptalksMap';

interface State {
    aircrafts: { [clientId: string]: AircraftStatus };
    myClientId: string | null;
    showPathClientIds: string[];
    followingClientId: string | null;
    moreInfoClientIds: string[];
    flightPlanClientId: string | null;

    map3D: boolean;
    mapTileType: MapTileType;
}

export class Home extends React.Component<any, State> {
    static displayName = Home.name;

    private hub: signalr.HubConnection;
    private map: IMap;
    private currentView?: View;

    private aircrafts: { [clientId: string]: { aircraftStatus: AircraftStatus, lastUpdated: Date } } = {};

    constructor(props: any) {
        super(props);

        this.state = {
            aircrafts: {},
            myClientId: null,
            showPathClientIds: [],
            followingClientId: null,
            moreInfoClientIds: [],
            flightPlanClientId: null,
            map3D: false,
            mapTileType: MapTileType.OpenStreetMap,
        }

        this.map = new LeafletMap();
        this.map.onViewChanged(view => {
            this.currentView = view;
        });

        this.hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub?clientType=Web')
            .withAutomaticReconnect()
            //.withHubProtocol(new protocol.MessagePackHubProtocol())
            .build();

        this.handleAircraftClick = this.handleAircraftClick.bind(this);

        this.handleMap2D = this.handleMap2D.bind(this);
        this.handleMap3D = this.handleMap3D.bind(this);

        this.handleOpenStreetMap = this.handleOpenStreetMap.bind(this);
        this.handleOpenTopoMap = this.handleOpenTopoMap.bind(this);
        this.handleEsriWorldImagery = this.handleEsriWorldImagery.bind(this);
        this.handleEsriTopo = this.handleEsriTopo.bind(this);
        this.handleUsVfrSectional = this.handleUsVfrSectional.bind(this);

        this.handleMeChanged = this.handleMeChanged.bind(this);
        this.handleShowPathChanged = this.handleShowPathChanged.bind(this);
        this.handleFollowingChanged = this.handleFollowingChanged.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
        this.handleFlightPlanChanged = this.handleFlightPlanChanged.bind(this);

        this.handleAirportsLoaded = this.handleAirportsLoaded.bind(this);
        this.handleFlightPlansLoaded = this.handleFlightPlansLoaded.bind(this);
        this.cleanUp = this.cleanUp.bind(this);
    }

    async componentDidMount() {
        this.initializeMap();

        const hub = this.hub;

        hub.onreconnected(async connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);

            await hub.send('Join', 'Map');
        })

        hub.on("UpdateATC", (clientId, status: ATCStatus, atc: ATCInfo) => {
            try {
                this.map.moveATCMarker(clientId, status, atc);
            } catch (e) {
                console.error(e);
            }
        });

        hub.on("UpdateAircraft", (clientId, aircraftStatus: AircraftStatus) => {
            try {
                aircraftStatus.isReady = !(Math.abs(aircraftStatus.latitude) < 0.02 && Math.abs(aircraftStatus.longitude) < 0.02);

                this.setState({
                    aircrafts: {
                        ...this.state.aircrafts,
                        [clientId]: aircraftStatus
                    }
                });

                this.aircrafts[clientId] = {
                    lastUpdated: new Date(),
                    aircraftStatus: aircraftStatus
                };

                if (aircraftStatus.isReady) {
                    this.map.moveMarker(clientId, aircraftStatus, this.state.myClientId === clientId, clientId === this.state.followingClientId, this.state.moreInfoClientIds.includes(clientId));

                    if (this.state.showPathClientIds.includes(clientId)) {
                        this.map.track(clientId, aircraftStatus);
                    }
                } else {
                    // Aircraft not loaded
                    if (this.state.showPathClientIds.includes(clientId)) {
                        this.map.clearTrack(clientId);
                    }
                    this.map.cleanUp(clientId, this.state.myClientId === clientId);
                }
            } catch (e) {
                console.error(e);
            }
        });

        hub.on("ReturnFlightPlanDetails", (connectionId, flightPlan: FlightPlanData | null) => {
            if (flightPlan) {
                this.map.drawFlightPlans([flightPlan]);
            }
        });

        await hub.start();

        setInterval(this.cleanUp, 2000);
    }

    private initializeMap() {
        this.map.initialize('mapid', this.currentView);
        this.map.setTileLayer(this.state.mapTileType);
    }

    private cleanUp() {
        const clientIds = Object.keys(this.aircrafts);
        for (let clientId of clientIds) {
            const aircraft = this.aircrafts[clientId];
            if (new Date().getTime() - aircraft.lastUpdated.getTime() > 5 * 1000) {
                this.map.cleanUp(clientId, clientId === this.state.myClientId);

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
                delete newAircrafts[clientId];
                this.setState({
                    aircrafts: newAircrafts
                })

                delete this.aircrafts[clientId];
            }
        }
    }

    private handleAircraftClick(clientId: string) {
        if (this.map) {
            this.map.focusAircraft(this.aircrafts[clientId].aircraftStatus);
        }
    }

    private handleMap2D() {
        this.setState({
            map3D: false
        });

        this.map?.deinitialize();
        this.map = new LeafletMap();
        this.map.onViewChanged(view => {
            this.currentView = view;
        });
        this.initializeMap();
    }

    private handleMap3D() {
        this.setState({
            map3D: true
        });

        this.map?.deinitialize();
        this.map = new MaptalksMap();
        this.map.onViewChanged(view => {
            this.currentView = view;
        });
        this.initializeMap();
    }

    private handleOpenStreetMap() {
        this.setState({
            mapTileType: MapTileType.OpenStreetMap
        })
        this.map.setTileLayer(MapTileType.OpenStreetMap);
    }

    private handleOpenTopoMap() {
        this.setState({
            mapTileType: MapTileType.OpenTopoMap
        })
        this.map.setTileLayer(MapTileType.OpenTopoMap);
    }

    private handleEsriWorldImagery() {
        this.setState({
            mapTileType: MapTileType.EsriWorldImagery
        })
        this.map.setTileLayer(MapTileType.EsriWorldImagery);
    }

    private handleEsriTopo() {
        this.setState({
            mapTileType: MapTileType.EsriTopo
        })
        this.map.setTileLayer(MapTileType.EsriTopo);
    }

    private handleUsVfrSectional() {
        this.setState({
            mapTileType: MapTileType.UsVfrSectional
        })
        this.map.setTileLayer(MapTileType.UsVfrSectional);
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

    render() {
        return <>
            <div id="mapid" style={{ height: '100%' }}></div>
            <TypeWrapper className="btn-group-vertical">
                <ButtonGroup>
                    <Button className="btn btn-light" active={!this.state.map3D} onClick={this.handleMap2D}>2D</Button>
                    <Button className="btn btn-light" active={this.state.map3D} onClick={this.handleMap3D}>3D</Button>
                </ButtonGroup>
            </TypeWrapper>
            <LayerWrapper className="btn-group-vertical">
                <Button className="btn btn-light" active={this.state.mapTileType === MapTileType.OpenStreetMap} onClick={this.handleOpenStreetMap}>OpenStreetMap</Button>
                <Button className="btn btn-light" active={this.state.mapTileType === MapTileType.OpenTopoMap} onClick={this.handleOpenTopoMap}>OpenTopoMap</Button>
                <Button className="btn btn-light" active={this.state.mapTileType === MapTileType.EsriWorldImagery} onClick={this.handleEsriWorldImagery}>Esri Imagery</Button>
                <Button className="btn btn-light" active={this.state.mapTileType === MapTileType.EsriTopo} onClick={this.handleEsriTopo}>Esri Topo</Button>
                <Button className="btn btn-light" active={this.state.mapTileType === MapTileType.UsVfrSectional} onClick={this.handleUsVfrSectional}>US VFR</Button>
            </LayerWrapper>
            <AircraftList aircrafts={this.state.aircrafts} onAircraftClick={this.handleAircraftClick}
                onMeChanged={this.handleMeChanged} myClientId={this.state.myClientId}
                onShowPathChanged={this.handleShowPathChanged} showPathClientIds={this.state.showPathClientIds}
                onFollowingChanged={this.handleFollowingChanged} followingClientId={this.state.followingClientId}
                onMoreInfoChanged={this.handleMoreInfoChanged} moreInfoClientIds={this.state.moreInfoClientIds}
                onFlightPlanChanged={this.handleFlightPlanChanged} flightPlanClientId={this.state.flightPlanClientId}
            />
            <EventList onAirportsLoaded={this.handleAirportsLoaded} onFlightPlansLoaded={this.handleFlightPlansLoaded} />
        </>;
    }
}

const LayerWrapper = styled.div`
position: absolute;
top: 130px;
left: 10px;
z-index: 1000;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;

button {
    display: block;
}
`;

const TypeWrapper = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 1000;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
`;