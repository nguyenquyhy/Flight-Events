import { IMap, MapTileType } from './IMap';
import * as maptalks from 'maptalks';
import { AircraftStatus, Airport, FlightPlan } from '../Models';
import { MAPBOX_API_KEY } from '../Constants';

interface Markers {
    aircraft: Sector
    aircraftLine: Marker
    //info: any
}

interface Coordinate {
    sub: (other: Coordinate) => any
}

interface Map {
    panTo: (latlng: Coordinate) => void;
    setPitch: (pitch: number) => void;
    setBaseLayer: (layer: any) => void;
    remove: () => void;
    on: (events: string, handler: () => void) => void;
    getZoom: () => number;
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
    setRadius: (radius: number) => void;
}

export default class MaptalksMap implements IMap {
    static AIRCRAFT_SIZE = 120;
    static ANIMATION_DURATION = 500;
    static FEET_TO_METER = .3048;

    map: Map | undefined;
    markers: { [connectionId: string]: Markers } = {};

    visibleCircle = new maptalks.Circle([0, 0], 3048, {
        symbol: {
            lineColor: '#000000',
            lineWidth: 2,
            lineOpacity: 0.25,
            polygonFill: 'none'
        }
    })

    aircraftLayer: VectorLayer | undefined;

    initialize(divId: string) {
        const map = new maptalks.Map(divId, {
            center: [-0.113049, 51.498568],
            zoom: 14
        });

        map.on('zoomend', () => this.handleZoom());

        this.aircraftLayer = new maptalks.VectorLayer('vector', {
            enableAltitude: true,
            // draw altitude
            drawAltitude: {
                lineWidth: 1,
                lineColor: '#aaa'
            }
        }).addTo(map);

        this.map = map;

        this.aircraftLayer!!.addGeometry(this.visibleCircle!!);
        this.visibleCircle.hide();
    }

    private handleZoom() {
        if (!this.map) return;

        const radius = Math.pow(2, 14 - this.map.getZoom()) * MaptalksMap.AIRCRAFT_SIZE;
        for (let connectionId in this.markers) {
            const marker = this.markers[connectionId];
            marker.aircraft.setRadius(radius);
        }
    }

    deinitialize() {
        if (this.map) {
            this.map.remove();
        }
    }

    public setTileLayer(type: MapTileType) {
        if (!this.map) return;

        switch (type) {
            case MapTileType.OpenStreetMap:
                this.map.setBaseLayer(new maptalks.TileLayer('openstreetmap', {
                    urlTemplate: 'https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}?access_token=' + MAPBOX_API_KEY,//{ accessToken }',
                    maxZoom: 18,
                    attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors, <a href="https://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, Imagery © <a href="https://www.mapbox.com/">Mapbox</a>',
                }));
                break;
            case MapTileType.OpenTopoMap:
                this.map.setBaseLayer(new maptalks.TileLayer('opentopomap', {
                    urlTemplate: 'https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c'],
                    maxZoom: 17,
                    attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
                }));
                break;
            case MapTileType.EsriWorldImagery:
                this.map.setBaseLayer(new maptalks.TileLayer('esri', {
                    urlTemplate: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
                    attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community'
                }));
                break;
            case MapTileType.EsriTopo:
                this.map.setBaseLayer(new maptalks.TileLayer('esritopo', {
                    urlTemplate: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}', 
                    attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ, TomTom, Intermap, iPC, USGS, FAO, NPS, NRCAN, GeoBase, Kadaster NL, Ordnance Survey, Esri Japan, METI, Esri China (Hong Kong), and the GIS User Community'
                }));
                break;
            case MapTileType.Carto:
                this.map.setBaseLayer(new maptalks.TileLayer('carto', {
                    urlTemplate: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c', 'd'],
                    attribution: '&copy; <a href="http://osm.org">OpenStreetMap</a> contributors, &copy; <a href="https://carto.com/">CARTO</a>'
                }));
                break;
        }
    }

    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isMoreInfo: boolean) {
        if (!this.map || !this.aircraftLayer) return;

        const iconSize = 12
        const infoBoxWidth = 100

        let latlng: Coordinate = new maptalks.Coordinate([aircraftStatus.Longitude, aircraftStatus.Latitude]);

        if (Object.keys(this.markers).length === 0) {
            // Move to 1st aircraft
            this.map.panTo(latlng);
            this.map.setPitch(30);
        } else if (isFollowing) {
            this.map.panTo(latlng);
        }

        let markers = this.markers[connectionId];
        if (markers) {
            // Existing marker
            const altitude = aircraftStatus.Altitude * MaptalksMap.FEET_TO_METER

            const offset = latlng.sub(markers.aircraft.getCoordinates());

            markers.aircraft.setProperties({ altitude: altitude });
            markers.aircraft.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });
            markers.aircraft.setStartAngle(-aircraftStatus.TrueHeading - 10 - 90);
            markers.aircraft.setEndAngle(-aircraftStatus.TrueHeading + 10 - 90);

            markers.aircraftLine.setProperties({ altitude: altitude });
            markers.aircraftLine.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });

            if (isMe) {
                const circleOffset = latlng.sub(this.visibleCircle.getCoordinates());
                this.visibleCircle.setProperties({ altitude: altitude });
                this.visibleCircle.animate({ translate: [circleOffset['x'], circleOffset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });
            }

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
                    'markerPath': 'M',
                    'markerFill': 'rgb(216,115,149)',
                    'markerLineColor': '#aaa',
                    'markerPathWidth': 305,
                    'markerPathHeight': 313,
                    'markerWidth': 30,
                    'markerHeight': 42,

                    'markerRotation': -aircraftStatus.TrueHeading
                },
                properties: {
                    altitude: aircraftStatus.Altitude * MaptalksMap.FEET_TO_METER
                },
                //icon: L.icon({
                //    iconUrl: 'marker-aircraft.png',
                //    iconSize: [10, 30],
                //    iconAnchor: [5, 25],
                //}),
                //zIndexOffset: 2000
            })
            const aircraft = new maptalks.Sector(latlng, MaptalksMap.AIRCRAFT_SIZE, -aircraftStatus.TrueHeading - 10 - 90, -aircraftStatus.TrueHeading + 10 - 90, {
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
        this.visibleCircle.show();
    }

    removeRangeCircle() {
        this.visibleCircle.hide();
    }


}