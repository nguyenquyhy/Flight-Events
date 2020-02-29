import * as React from 'react';
import styled from 'styled-components';
import * as L from 'leaflet';
import 'leaflet-rotatedmarker';
import * as signalr from '@microsoft/signalr';
import { AircraftStatus, Airport } from '../Models';
import AircraftList from './AircraftList';
import EventList from './EventList';
import { MAPBOX_API_KEY } from '../Constants';

enum MapTileType {
    OpenStreetMap,
    OpenTopoMap,
    EsriWorldImagery,
    EsriTopo
}

interface State {
    aircrafts: { [connectionId: string]: AircraftStatus };
    myConnectionId: string | null;
    followingConnectionId: string | null;
    moreInfoConnectionIds: string[];
}

interface Markers {
    lastUpdated: Date;
    aircraft: L.Marker<any>
    info: L.Marker<any>
}

export class Home extends React.Component<any, State> {
    static displayName = Home.name;

    mymap: L.Map;
    baseLayerGroup: L.LayerGroup;
    airportLayerGroup: L.LayerGroup;
    markers: { [connectionId: string]: Markers } = {};
    airportMarkers: { [indent: string]: L.Marker } = {};

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
        this.cleanUp = this.cleanUp.bind(this);
    }

    componentDidMount() {
        this.mymap = L.map('mapid').setView([51.505, -0.09], 13);

        this.baseLayerGroup = L.layerGroup().addTo(this.mymap);
        this.airportLayerGroup = L.layerGroup().addTo(this.mymap);
        this.setTileLayer(MapTileType.OpenStreetMap);

        let hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub')
            .withAutomaticReconnect()
            .build();

        hub.onreconnected(connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);
        })

        hub.on("UpdateAircraft", (connectionId, aircraftStatus: AircraftStatus) => {
            this.setState({
                aircrafts: {
                    ...this.state.aircrafts,
                    [connectionId]: aircraftStatus
                }
            });

            let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];

            if (Object.keys(this.markers).length === 0) {
                this.mymap.setView(latlng, 11);
            } else if (connectionId === this.state.followingConnectionId) {
                this.mymap.setView(latlng, this.mymap.getZoom());
            }

            let markers = this.markers[connectionId];
            if (markers) {
                markers.lastUpdated = new Date();
                markers.aircraft.setLatLng(latlng);
                markers.info.setLatLng(latlng);

                let className = 'divicon-aircraft-info';
                if (connectionId === this.state.myConnectionId) className += " me";

                if (this.state.moreInfoConnectionIds.includes(connectionId)) {
                    if (aircraftStatus.trueHeading >= 180) {
                        markers.info.setIcon(L.divIcon({
                            className: className,
                            html: `<div>${aircraftStatus.callsign}</div><div>${Math.floor(aircraftStatus.altitude)}FT</div><div>${Math.floor(aircraftStatus.heading)}\u00B0</div><div>${Math.floor(aircraftStatus.indicatedAirSpeed)}KTS</div>`,
                            iconSize: [12, 60],
                            iconAnchor: [-12, 10],
                        }))
                    } else {
                        markers.info.setIcon(L.divIcon({
                            className: className + ' right',
                            html: `<div>${aircraftStatus.callsign}</div><div>${Math.floor(aircraftStatus.altitude)}FT</div><div>${Math.floor(aircraftStatus.heading)}\u00B0</div><div>${Math.floor(aircraftStatus.indicatedAirSpeed)}KTS</div>`,
                            iconSize: [12, 60],
                            iconAnchor: [72, 10],
                        }))
                    }
                } else {
                    if (aircraftStatus.trueHeading >= 180) {
                        markers.info.setIcon(L.divIcon({
                            className: className,
                            html: `<div>${aircraftStatus.callsign}</div>`,
                            iconSize: [12, 60],
                            iconAnchor: [-12, 10],
                        }))
                    } else {
                        markers.info.setIcon(L.divIcon({
                            className: className + ' right',
                            html: `<div>${aircraftStatus.callsign}</div>`,
                            iconSize: [12, 60],
                            iconAnchor: [72, 10],
                        }))
                    }
                }
            } else {
                const aircraft = L.marker(latlng, {
                    icon: L.icon({
                        iconUrl: 'marker-aircraft.png',
                        iconSize: [10, 30],
                        iconAnchor: [5, 25],
                    }),
                    zIndexOffset: 2000
                }).addTo(this.mymap);
                const info = L.marker(latlng, {
                    icon: L.divIcon({
                        className: 'divicon-aircraft-info',
                        html: `<div style='width: 50px'>${aircraftStatus.callsign}</div>`,
                        iconSize: [12, 50],
                        iconAnchor: [-12, -4],
                    }),
                    zIndexOffset: 1000
                }).addTo(this.mymap)
                markers = {
                    lastUpdated: new Date(),
                    aircraft: aircraft,
                    info: info
                }
                this.markers[connectionId] = markers;
            }

            let popup = `Altitude: ${Math.floor(aircraftStatus.altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.indicatedAirSpeed)}`;
            if (aircraftStatus.callsign) {
                popup = `<b>${aircraftStatus.callsign}</b><br />${popup}`;
            }

            markers.info.bindPopup(popup, {
                autoPan: false
            });
            markers.info.setIcon(markers.info.getIcon());
            markers.aircraft
                .bindPopup(popup)
                .setRotationAngle(aircraftStatus.trueHeading);
        });

        hub.start();

        setInterval(this.cleanUp, 2000);
    }

    private cleanUp() {
        if (this.mymap !== undefined && this.markers !== undefined) {
            const connectionIds = Object.keys(this.markers);
            for (let connectionId of connectionIds) {
                const marker = this.markers[connectionId];
                if (new Date().getTime() - marker.lastUpdated.getTime() > 5 * 1000) {
                    marker.aircraft.removeFrom(this.mymap);
                    marker.info.removeFrom(this.mymap);
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
                    delete this.markers[connectionId];
                }

            }
        }
    }

    private setTileLayer(type: MapTileType) {
        this.baseLayerGroup.clearLayers();
        switch (type) {
            case MapTileType.OpenStreetMap:
                this.baseLayerGroup.addLayer(L.tileLayer('https://api.mapbox.com/styles/v1/{id}/tiles/{z}/{x}/{y}?access_token={accessToken}', {
                    attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, <a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
                    maxZoom: 18,
                    id: 'mapbox/streets-v11',
                    tileSize: 512,
                    zoomOffset: -1,
                    accessToken: `${MAPBOX_API_KEY}`
                }));
                break;
            case MapTileType.OpenTopoMap:
                this.baseLayerGroup.addLayer(L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
                    maxZoom: 17,
                    attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
                }));
                break;
            case MapTileType.EsriWorldImagery:
                this.baseLayerGroup.addLayer(L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
                    attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community'
                }));
                break;
            case MapTileType.EsriTopo:
                this.baseLayerGroup.addLayer(L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}', {
                    attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ, TomTom, Intermap, iPC, USGS, FAO, NPS, NRCAN, GeoBase, Kadaster NL, Ordnance Survey, Esri Japan, METI, Esri China (Hong Kong), and the GIS User Community'
                }));
                break;
        }
    }

    private handleAircraftClick(connectionId: string, aircraftStatus: AircraftStatus) {
        if (this.mymap) {
            let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];
            this.mymap.setView(latlng, this.mymap.getZoom());
        }
    }

    private handleOpenStreetMap() {
        this.setTileLayer(MapTileType.OpenStreetMap);
    }

    private handleOpenTopoMap() {
        this.setTileLayer(MapTileType.OpenTopoMap);
    }

    private handleEsriWorldImagery() {
        this.setTileLayer(MapTileType.EsriWorldImagery);
    }

    private handleEsriTopo() {
        this.setTileLayer(MapTileType.EsriTopo);
    }

    private handleMeChanged(connectionId: string | null) {
        this.setState({ myConnectionId: connectionId });
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

    private handleAirportsLoaded(airports: Airport[]) {
        if (this.mymap) {
            const minLongitude = airports.reduce((prev, curr) => Math.min(prev, curr.longitude), 180);
            const maxLongitude = airports.reduce((prev, curr) => Math.max(prev, curr.longitude), -180);
            const minLatitude = airports.reduce((prev, curr) => Math.min(prev, curr.latitude), 90);
            const maxLatitude = airports.reduce((prev, curr) => Math.max(prev, curr.latitude), -90);

            this.mymap.fitBounds([[minLatitude, minLongitude], [maxLatitude, maxLongitude]]);

            for (let ident in this.airportMarkers) {
                this.airportMarkers[ident].removeFrom(this.mymap);
            }

            this.airportMarkers = {};

            for (let airport of airports) {
                const marker = L.marker([airport.latitude, airport.longitude], {
                    title: airport.name
                }).addTo(this.mymap);
                this.airportMarkers[airport.ident] = marker;
            }
        }
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
            <EventList onAirportsLoaded={this.handleAirportsLoaded}/>
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