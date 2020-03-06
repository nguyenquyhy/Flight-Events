import * as React from 'react';
import styled from 'styled-components';
import * as signalr from '@microsoft/signalr';
import 'msgpack5';
import * as protocol from '@microsoft/signalr-protocol-msgpack';
import { AircraftStatus, Airport, FlightPlan } from '../Models';
import AircraftList from './AircraftList';
import EventList from './EventList';
import { IMap } from '../maps/IMap';
import LeafletMap, { MapTileType } from '../maps/LeaftletMap';
import MaptalksMap from '../maps/MaptalksMap';

interface State {
    aircrafts: { [connectionId: string]: AircraftStatus };
    myConnectionId: string | null;
    followingConnectionId: string | null;
    moreInfoConnectionIds: string[];
}

export class Home extends React.Component<any, State> {
    static displayName = Home.name;

    //private leafletMap: IMap = new LeafletMap();
    private leafletMap: IMap = new MaptalksMap();

    private aircrafts: { [connectionId: string]: { aircraftStatus: AircraftStatus, lastUpdated: Date } } = {};

    constructor(props: any) {
        super(props);

        this.state = {
            aircrafts: {},
            myConnectionId: null,
            followingConnectionId: null,
            moreInfoConnectionIds: []
        }

        this.handleAircraftClick = this.handleAircraftClick.bind(this);
        this.handleOpenStreetMap = this.handleOpenStreetMap.bind(this);
        this.handleOpenTopoMap = this.handleOpenTopoMap.bind(this);
        this.handleEsriWorldImagery = this.handleEsriWorldImagery.bind(this);
        this.handleEsriTopo = this.handleEsriTopo.bind(this);
        this.handleMeChanged = this.handleMeChanged.bind(this);
        this.handleFollowingChanged = this.handleFollowingChanged.bind(this);
        this.handleAirportsLoaded = this.handleAirportsLoaded.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
        this.handleFlightPlansLoaded = this.handleFlightPlansLoaded.bind(this);
        this.cleanUp = this.cleanUp.bind(this);
    }

    async componentDidMount() {
        this.leafletMap.initialize('mapid');

        //this.leafletMap.setTileLayer(MapTileType.OpenStreetMap);

        let hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub')
            .withAutomaticReconnect()
            .withHubProtocol(new protocol.MessagePackHubProtocol())
            .build();

        hub.onreconnected(async connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);

            await hub.send('Join', 'Map');
        })

        hub.on("UpdateAircraft", (connectionId, aircraftStatus: AircraftStatus) => {
            this.setState({
                aircrafts: {
                    ...this.state.aircrafts,
                    [connectionId]: aircraftStatus
                }
            });

            this.aircrafts[connectionId] = {
                lastUpdated: new Date(),
                aircraftStatus: aircraftStatus
            };

            this.leafletMap.moveMarker(connectionId, aircraftStatus, this.state.myConnectionId === connectionId, connectionId === this.state.followingConnectionId, this.state.moreInfoConnectionIds.includes(connectionId));
        });

        await hub.start();

        await hub.send('Join', 'Map');

        setInterval(this.cleanUp, 2000);
    }

    private cleanUp() {
        const connectionIds = Object.keys(this.aircrafts);
        for (let connectionId of connectionIds) {
            const aircraft = this.aircrafts[connectionId];
            if (new Date().getTime() - aircraft.lastUpdated.getTime() > 5 * 1000) {
                this.leafletMap.cleanUp(connectionId, connectionId === this.state.myConnectionId);

                if (connectionId === this.state.myConnectionId) {
                    this.setState({
                        myConnectionId: null
                    });
                }
                if (connectionId === this.state.followingConnectionId) {
                    this.setState({
                        followingConnectionId: null
                    });
                }
                let newAircrafts = {
                    ...this.state.aircrafts
                };
                delete newAircrafts[connectionId];
                this.setState({
                    aircrafts: newAircrafts
                })

                delete this.aircrafts[connectionId];
            }
        }
    }

    private handleAircraftClick(connectionId: string, aircraftStatus: AircraftStatus) {
        if (this.leafletMap) {
            this.leafletMap.forcusAircraft(aircraftStatus);
        }
    }

    private handleOpenStreetMap() {
        //this.leafletMap.setTileLayer(MapTileType.OpenStreetMap);
    }

    private handleOpenTopoMap() {
        //this.leafletMap.setTileLayer(MapTileType.OpenTopoMap);
    }

    private handleEsriWorldImagery() {
        //this.leafletMap.setTileLayer(MapTileType.EsriWorldImagery);
    }

    private handleEsriTopo() {
        //this.leafletMap.setTileLayer(MapTileType.EsriTopo);
    }

    private handleMeChanged(connectionId: string | null) {
        this.setState({ myConnectionId: connectionId });

        if (connectionId) {
            this.leafletMap.addRangeCircle();
        } else {
            this.leafletMap.removeRangeCircle();
        }
    }

    private handleFollowingChanged(connectionId: string | null) {
        this.setState({ followingConnectionId: connectionId });
    }

    private handleMoreInfoChanged(connectionId: string) {
        if (this.state.moreInfoConnectionIds.includes(connectionId)) {
            this.setState({ moreInfoConnectionIds: this.state.moreInfoConnectionIds.filter(o => o !== connectionId) });
        } else {
            this.setState({ moreInfoConnectionIds: this.state.moreInfoConnectionIds.concat(connectionId) });
        }
    }

    public handleAirportsLoaded(airports: Airport[]) {
        this.leafletMap.drawAirports(airports);
    }

    public handleFlightPlansLoaded(flightPlans: FlightPlan[]) {
        this.leafletMap.drawFlightPlans(flightPlans);
    }

    render() {
        return <>
            <div id="mapid" style={{ height: '100%' }}></div>
            <LayerWrapper className="btn-group-vertical">
                <TileButton className="btn btn-light" onClick={this.handleOpenStreetMap}>OpenStreetMap</TileButton>
                <TileButton className="btn btn-light" onClick={this.handleOpenTopoMap}>OpenTopoMap</TileButton>
                <TileButton className="btn btn-light" onClick={this.handleEsriWorldImagery}>Esri Imagery</TileButton>
                <TileButton className="btn btn-light" onClick={this.handleEsriTopo}>Esri Topo</TileButton>
            </LayerWrapper>
            <AircraftList aircrafts={this.state.aircrafts} onAircraftClick={this.handleAircraftClick}
                onMeChanged={this.handleMeChanged} myConnectionId={this.state.myConnectionId}
                onFollowingChanged={this.handleFollowingChanged} followingConnectionId={this.state.followingConnectionId}
                onMoreInfoChanged={this.handleMoreInfoChanged} moreInfoConnectionIds={this.state.moreInfoConnectionIds}
            />
            <EventList onAirportsLoaded={this.handleAirportsLoaded} onFlightPlansLoaded={this.handleFlightPlansLoaded} />
        </>;
    }
}

const LayerWrapper = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 1000;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;

button {
    display: block;
}
`;

const TileButton = styled.button`
`