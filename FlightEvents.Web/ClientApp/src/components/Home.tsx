import * as React from 'react';
import * as L from 'leaflet';
import 'leaflet-rotatedmarker';
import * as signalr from '@microsoft/signalr';
import { AircraftStatus } from '../Models';
import AircraftList from './AircraftList';

interface State {
    aircrafts: { [connectionId: string]: AircraftStatus };
}

export class Home extends React.Component<any, State> {
    static displayName = Home.name;

    mymap: L.Map;
    markers: { [connectionId: string]: L.Marker<any> } = {}

    constructor(props: any) {
        super(props);

        this.state = {
            aircrafts: {}
        }

        this.handleAircraftClick = this.handleAircraftClick.bind(this);
    }

    componentDidMount() {
        this.mymap = L.map('mapid').setView([51.505, -0.09], 13);
        L.tileLayer('https://api.mapbox.com/styles/v1/{id}/tiles/{z}/{x}/{y}?access_token={accessToken}', {
            attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, <a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
            maxZoom: 18,
            id: 'mapbox/streets-v11',
            tileSize: 512,
            zoomOffset: -1,
            accessToken: 'your.mapbox.access.token'
        }).addTo(this.mymap);

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
            })

            let marker = this.markers[connectionId];
            let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];
            if (marker) {
                marker.setLatLng(latlng);
            } else {
                marker = L.marker(latlng, {
                    icon: L.icon({
                        iconUrl: 'marker-aircraft.png',
                        iconSize: [10, 30],
                        iconAnchor: [5, 25],
                    })
                });
                marker.addTo(this.mymap);
                this.markers[connectionId] = marker;
            }

            let popup = `Altitude: ${Math.floor(aircraftStatus.altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.indicatedAirSpeed)}`;
            if (aircraftStatus.callsign) {
                popup = `<b>${aircraftStatus.callsign}</b><br />${popup}`;
            }

            marker
                .bindPopup(popup)
                .setRotationAngle(aircraftStatus.trueHeading);
        });

        hub.start();
    }

    private handleAircraftClick(connectionId: string, aircraftStatus: AircraftStatus) {
        if (this.mymap) {
            let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];
            this.mymap.setView(latlng, 12);
        }
    }

    render() {
        return <>
            <div id="mapid" style={{ height: '100%' }}></div>
            <AircraftList aircrafts={this.state.aircrafts} onAircraftClick={this.handleAircraftClick} />
        </>;
    }
}
