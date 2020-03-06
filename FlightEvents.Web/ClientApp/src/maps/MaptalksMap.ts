import { IMap } from './IMap';
import * as maptalks from 'maptalks';
import { AircraftStatus, Airport, FlightPlan } from '../Models';

interface Markers {
    aircraft: Sector
    aircraftLine: Marker
    //info: any
}

type LatLng = number[]

interface Coordinate {
    sub: (other: Coordinate) => any
}

interface Map {
    panTo: (latlng: Coordinate) => void;
    setPitch: (pitch: number) => void;
}

interface VectorLayer {
    addGeometry: (markers: Marker[]) => void;
}

interface Marker {
    setCoordinates: (coordinate: Coordinate) => void;
    getCoordinates: () => Coordinate;
    setProperties: (props: any) => void;
    animate: (animation: any, props: any) => void;
    remove: () => void;
}

interface Sector extends Marker {
    setStartAngle: (angle: number) => void;
    setEndAngle: (angle: number) => void;
}

export default class MaptalksMap implements IMap {
    map: Map | undefined;
    markers: { [connectionId: string]: Markers } = {};

    aircraftLayer: VectorLayer | undefined;

    initialize(divId: string) {
        this.map = new maptalks.Map(divId, {
            center: [-0.113049, 51.498568],
            zoom: 14,
            baseLayer: new maptalks.TileLayer('base', {
                urlTemplate: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png',
                subdomains: ['a', 'b', 'c', 'd'],
                attribution: '&copy; <a href="http://osm.org">OpenStreetMap</a> contributors, &copy; <a href="https://carto.com/">CARTO</a>'
            }),
            projection: 'identity'
        });

        this.aircraftLayer = new maptalks.VectorLayer('vector', {
            enableAltitude: true,
            // draw altitude
            drawAltitude: {
                lineWidth: 1,
                lineColor: '#aaa'
            }
        }).addTo(this.map);
    }

    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isMoreInfo: boolean) {
        if (!this.map || !this.aircraftLayer) return;

        const iconSize = 12
        const infoBoxWidth = 100

        let latlng: Coordinate = new maptalks.Coordinate([aircraftStatus.Longitude, aircraftStatus.Latitude]);

        if (Object.keys(this.markers).length === 0) {
            // Move to 1st aircraft
            this.map.panTo(latlng);
            this.map.setPitch(60);
        } else if (isFollowing) {
            this.map.panTo(latlng);
        }

        let markers = this.markers[connectionId];
        if (markers) {
            // Existing marker

            const offset = latlng.sub(markers.aircraft.getCoordinates());

            markers.aircraft.setProperties({ altitude: aircraftStatus.Altitude * 0.3048 });
            markers.aircraft.animate({ translate: [offset['x'], offset['y']] }, { duration: 500 });
            markers.aircraft.setStartAngle(-aircraftStatus.TrueHeading - 10 - 90);
            markers.aircraft.setEndAngle(-aircraftStatus.TrueHeading + 10 - 90);

            markers.aircraftLine.setProperties({ altitude: aircraftStatus.Altitude * 0.3048 });
            markers.aircraftLine.animate({ translate: [offset['x'], offset['y']] }, { duration: 500 });

            //markers.info.setLatLng(latlng);

            //let className = 'divicon-aircraft-info';
            //if (isMe) className += " me";

            //let iconSizeValue: L.PointExpression = [iconSize, 60];

            //if (isMoreInfo) {
            //    let htmlBody = `<div>${aircraftStatus.Callsign}<br />ALT ${Math.round(aircraftStatus.Altitude)} ft<br />HDG ${Math.round(aircraftStatus.Heading)}\u00B0<br />IAS ${Math.round(aircraftStatus.IndicatedAirSpeed)} kts</div>`

            //    if (aircraftStatus.TrueHeading >= 180) {
            //        markers.info.setIcon(L.divIcon({
            //            className: className,
            //            html: htmlBody,
            //            iconSize: iconSizeValue,
            //            iconAnchor: [-iconSize, 10],
            //        }))
            //    } else {
            //        markers.info.setIcon(L.divIcon({
            //            className: className,
            //            html: htmlBody,
            //            iconSize: iconSizeValue,
            //            iconAnchor: [infoBoxWidth + iconSize, 10],
            //        }))
            //    }
            //} else {
            //    if (aircraftStatus.TrueHeading >= 180) {
            //        markers.info.setIcon(L.divIcon({
            //            className: className,
            //            html: `<div>${aircraftStatus.Callsign}</div>`,
            //            iconSize: iconSizeValue,
            //            iconAnchor: [-iconSize, 10],
            //        }))
            //    } else {
            //        markers.info.setIcon(L.divIcon({
            //            className: className + ' right',
            //            html: `<div>${aircraftStatus.Callsign}</div>`,
            //            iconSize: iconSizeValue,
            //            iconAnchor: [infoBoxWidth + iconSize, 10],
            //        }))
            //    }
            //}
        } else {
            const aircraftLine = new maptalks.Marker(latlng, {
                symbol: {
                    'markerType': 'path',
                    'markerPath': '',
                    'markerFill': 'rgb(216,115,149)',
                    'markerLineColor': '#aaa',
                    'markerPathWidth': 305,
                    'markerPathHeight': 313,
                    'markerWidth': 30,
                    'markerHeight': 42,

                    'markerRotation': -aircraftStatus.TrueHeading
                },
                properties: {
                    altitude: aircraftStatus.Altitude * 0.3048
                },
                //icon: L.icon({
                //    iconUrl: 'marker-aircraft.png',
                //    iconSize: [10, 30],
                //    iconAnchor: [5, 25],
                //}),
                //zIndexOffset: 2000
            })
            const aircraft = new maptalks.Sector(latlng, 20, -aircraftStatus.TrueHeading - 10 - 90, -aircraftStatus.TrueHeading + 10 - 90, {
                symbol: {
                    lineColor: '#34495e',
                    lineWidth: 2,
                    polygonFill: 'rgb(135,196,240)',
                    polygonOpacity: 0.4
                },
                properties: {
                    altitude: aircraftStatus.Altitude * 0.3048
                },
                //icon: L.icon({
                //    iconUrl: 'marker-aircraft.png',
                //    iconSize: [10, 30],
                //    iconAnchor: [5, 25],
                //}),
                //zIndexOffset: 2000
            })//.addTo(this.map);
            //const info = L.marker(latlng, {
            //    icon: L.divIcon({
            //        className: 'divicon-aircraft-info',
            //        html: `<div style='width: 50px'>${aircraftStatus.Callsign}</div>`,
            //        iconSize: [iconSize, 50],
            //        iconAnchor: [-iconSize, -4],
            //    }),
            //    zIndexOffset: 1000
            //}).addTo(this.mymap)
            markers = {
                aircraft: aircraft,
                aircraftLine: aircraftLine
                //info: info
            }
            this.markers[connectionId] = markers;

            this.aircraftLayer.addGeometry([].concat(aircraft, aircraftLine));
        }

        let popup = `Altitude: ${Math.floor(aircraftStatus.Altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.IndicatedAirSpeed)}`;
        if (aircraftStatus.Callsign) {
            popup = `<b>${aircraftStatus.Callsign}</b><br />${popup}`;
        }

        //markers.info.bindPopup(popup, {
        //    autoPan: false
        //});
        //markers.info.setIcon(markers.info.getIcon());
        //markers.aircraft
        //    .bindPopup(popup)
        //    .setRotationAngle(aircraftStatus.TrueHeading);

        //if (this.circleMarker && isMe) {
        //    this.circleMarker.setLatLng(latlng);
        //}
    }

    drawAirports(airports: Airport[]) {
        throw new Error("Method not implemented.");
    }

    drawFlightPlans(flightPlans: FlightPlan[]) {
        throw new Error("Method not implemented.");
    }

    forcusAircraft(aircraftStatus: AircraftStatus) {
        if (this.map) {
            this.map.panTo(new maptalks.Coordinate([aircraftStatus.Longitude, aircraftStatus.Latitude]));
        }
    }

    cleanUp(connectionId: string, isMe: boolean) {
        if (this.map) {
            const marker = this.markers[connectionId];
            marker.aircraft.remove();
            //marker.info.removeFrom(this.mymap);
            //if (isMe) {
            //    if (this.circleMarker) {
            //        this.circleMarker.removeFrom(this.mymap);
            //        this.circleMarker = null;
            //    }
            //}
            delete this.markers[connectionId];
        }
    }

    addRangeCircle() {
        throw new Error("Method not implemented.");
    }

    removeRangeCircle() {
        throw new Error("Method not implemented.");
    }


}