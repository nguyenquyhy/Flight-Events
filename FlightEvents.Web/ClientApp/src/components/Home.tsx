import * as React from 'react';
import styled from 'styled-components';
import { ButtonGroup, Button } from 'reactstrap';
import * as signalr from '@microsoft/signalr';
import 'msgpack5';
import * as protocol from '@microsoft/signalr-protocol-msgpack';
import { AircraftStatus, Airport, FlightPlan } from '../Models';
import AircraftList from './AircraftList';
import EventList from './EventList';
import { IMap, MapTileType } from '../maps/IMap';
import LeafletMap from '../maps/LeaftletMap';
import MaptalksMap from '../maps/MaptalksMap';

interface State {
    aircrafts: { [connectionId: string]: AircraftStatus };
    myConnectionId: string | null;
    followingConnectionId: string | null;
    moreInfoConnectionIds: string[];

    map3D: boolean;
    mapTileType: MapTileType;
}

export class Home extends React.Component<any, State> {
    static displayName = Home.name;

    private map: IMap = new LeafletMap();

    private aircrafts: { [connectionId: string]: { aircraftStatus: AircraftStatus, lastUpdated: Date } } = {};

    constructor(props: any) {
        super(props);

        this.state = {
            aircrafts: {},
            myConnectionId: null,
            followingConnectionId: null,
            moreInfoConnectionIds: [],
            map3D: false,
            mapTileType: MapTileType.OpenStreetMap,
        }

        this.handleAircraftClick = this.handleAircraftClick.bind(this);

        this.handleMap2D = this.handleMap2D.bind(this);
        this.handleMap3D = this.handleMap3D.bind(this);

        this.handleOpenStreetMap = this.handleOpenStreetMap.bind(this);
        this.handleOpenTopoMap = this.handleOpenTopoMap.bind(this);
        this.handleEsriWorldImagery = this.handleEsriWorldImagery.bind(this);
        this.handleEsriTopo = this.handleEsriTopo.bind(this);
        this.handleUsVfrSectional = this.handleUsVfrSectional.bind(this);
        this.handleMeChanged = this.handleMeChanged.bind(this);
        this.handleFollowingChanged = this.handleFollowingChanged.bind(this);
        this.handleAirportsLoaded = this.handleAirportsLoaded.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
        this.handleFlightPlansLoaded = this.handleFlightPlansLoaded.bind(this);
        this.cleanUp = this.cleanUp.bind(this);
    }

    async componentDidMount() {
        this.initializeMap();

        let hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub')
            .withAutomaticReconnect()
            //.withHubProtocol(new protocol.MessagePackHubProtocol())
            .build();

        hub.onreconnected(async connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);

            await hub.send('Join', 'Map');
        })

        hub.on("UpdateAircraft", (connectionId, aircraftStatus: AircraftStatus) => {
            try {
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

                this.map.moveMarker(connectionId, aircraftStatus, this.state.myConnectionId === connectionId, connectionId === this.state.followingConnectionId, this.state.moreInfoConnectionIds.includes(connectionId));
            } catch (e) {
                console.error(e);
            }
        });

        await hub.start();

        await hub.send('Join', 'Map');

        setInterval(this.cleanUp, 2000);
    }

    private initializeMap() {
        this.map.initialize('mapid');
        this.map.setTileLayer(this.state.mapTileType);
    }

    private cleanUp() {
        const connectionIds = Object.keys(this.aircrafts);
        for (let connectionId of connectionIds) {
            const aircraft = this.aircrafts[connectionId];
            if (new Date().getTime() - aircraft.lastUpdated.getTime() > 5 * 1000) {
                this.map.cleanUp(connectionId, connectionId === this.state.myConnectionId);

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
        if (this.map) {
            this.map.focusAircraft(aircraftStatus);
        }
    }

    private handleMap2D() {
        this.setState({
            map3D: false
        });

        this.map.deinitialize();
        this.map = new LeafletMap();
        this.initializeMap();
    }

    private handleMap3D() {
        this.setState({
            map3D: true
        });

        this.map.deinitialize();
        this.map = new MaptalksMap();
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

    private handleMeChanged(connectionId: string | null) {
        this.setState({ myConnectionId: connectionId });

        if (connectionId) {
            this.map.addRangeCircle();
        } else {
            this.map.removeRangeCircle();
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
        this.map.drawAirports(airports);
    }

    public handleFlightPlansLoaded(flightPlans: FlightPlan[]) {
        this.map.drawFlightPlans(flightPlans);
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