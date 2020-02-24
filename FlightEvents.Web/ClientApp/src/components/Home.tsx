import * as React from 'react';
import * as L from 'leaflet';
import 'leaflet-rotatedmarker';
import * as signalr from '@microsoft/signalr';

interface AircraftStatus {
    longitude: number;
    latitude: number;
    heading: number;
    trueHeading: number;

    altitude: number;
    altitudeAboveGround: number;
    indicatedAirSpeed: number;
}

export class Home extends React.Component {
    static displayName = Home.name;

    markers: { [connectionId: string]: L.Marker<any> } = {}

    componentDidMount() {
        let mymap = L.map('mapid').setView([51.505, -0.09], 13);
        L.tileLayer('https://api.mapbox.com/styles/v1/{id}/tiles/{z}/{x}/{y}?access_token={accessToken}', {
            attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, <a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
            maxZoom: 18,
            id: 'mapbox/streets-v11',
            tileSize: 512,
            zoomOffset: -1,
            accessToken: 'your.mapbox.access.token'
        }).addTo(mymap);

        let hub = new signalr.HubConnectionBuilder()
            .withUrl('/FlightEventHub')
            .withAutomaticReconnect()
            .build();

        hub.onreconnected(connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);
        })

        hub.on("UpdateAircraft", (connectionId, aircraftStatus: AircraftStatus) => {
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
                marker.addTo(mymap);
                this.markers[connectionId] = marker;
            }
            marker
                .bindPopup(`Altitude: ${Math.floor(aircraftStatus.altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.indicatedAirSpeed)}`)
                .setRotationAngle(aircraftStatus.trueHeading);
            mymap.setView(latlng, 12);
        });

        hub.start();
    }

    render() {
        return (
            <div id="mapid" style={{ height: 600 }}></div>
        );
    }
}
