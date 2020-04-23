﻿import { IMap, MapTileType, OnViewChangedFn, View } from './IMap';
import * as L from 'leaflet';
import 'leaflet-rotatedmarker';
import { AircraftStatus, Airport, FlightPlanData, ATCStatus, ATCInfo } from '../Models';


interface Markers {
    aircraft: L.Marker<any>
    info: L.Marker<any>
}

export default class LeafletMap implements IMap {
    mymap: L.Map;
    baseLayerGroup: L.LayerGroup;
    airportLayerGroup: L.LayerGroup;
    airportMarkers: { [indent: string]: L.Marker } = {};

    markers: { [connectionId: string]: Markers } = {};
    atcMarkers: { [connectionId: string]: L.Marker } = {};

    flightPlanLayerGroup: L.LayerGroup;

    circleMarker: L.Circle;

    onViewChangedHandler: OnViewChangedFn | null = null;

    public initialize(divId: string, view?: View) {
        this.mymap = L.map(divId).setView(view ? [view.latitude, view.longitude] : [51.505, -0.09], view ? view.zoom : 13);
        this.mymap.on('moveend', (e) => {
            const zoom = this.mymap.getZoom();
            const center = this.mymap.getCenter();
            if (this.onViewChangedHandler) {
                this.onViewChangedHandler({ latitude: center.lat, longitude: center.lng, zoom: zoom });
            }
        });

        this.baseLayerGroup = L.layerGroup().addTo(this.mymap);
        this.airportLayerGroup = L.layerGroup().addTo(this.mymap);
        this.flightPlanLayerGroup = L.layerGroup().addTo(this.mymap);

        setInterval(this.cleanUp, 2000);
    }

    public deinitialize() {
        this.onViewChangedHandler = null;
        this.mymap.remove();
    }

    public setTileLayer(type: MapTileType) {
        this.baseLayerGroup.clearLayers();
        switch (type) {
            case MapTileType.OpenStreetMap:
                this.baseLayerGroup.addLayer(L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
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
            case MapTileType.UsVfrSectional:
                this.baseLayerGroup.addLayer(L.tileLayer('https://wms.chartbundle.com/tms/v1.0/sec/{z}/{x}/{y}.png?type=google', {
                    maxZoom: 12,
                    attribution: 'Map data: &copy; Federal Aviation Administration (FAA), <a href="http://chartbundle.com">ChartBundle.com</a>'
                }));
                break;
        }
    }

    public moveATCMarker(connectionId: string, status: ATCStatus | null, info: ATCInfo | null) {
        if (status && info) {
            const latlng: L.LatLngExpression = [status.latitude, status.longitude];

            const marker = this.atcMarkers[connectionId];
            if (marker) {
                // Existing marker
                marker.setLatLng(latlng);
            } else {
                this.atcMarkers[connectionId] = L.marker(latlng, {
                    icon: L.icon({
                        iconUrl: 'marker-tower.png',
                        iconSize: [30, 30],
                        iconAnchor: [15, 15],
                    }),
                    zIndexOffset: 2000
                })
                    .bindPopup(`<strong>${info.callsign} [${(status.frequencyCom / 1000)}]</strong><br />Name: ${info.realName}<br />Certificate: ${info.certificate}`)
                    .addTo(this.mymap);
            }
        } else {
            // Remove
            const marker = this.atcMarkers[connectionId];
            if (marker) {
                marker.remove();
                delete this.atcMarkers[connectionId];
            }
        }
    }

    public moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isMoreInfo: boolean) {
        const iconSize = 12
        const infoBoxWidth = 100

        let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];

        if (Object.keys(this.markers).length === 0) {
            // Move to 1st aircraft
            this.mymap.setView(latlng, 11);
        } else if (isFollowing) {
            this.mymap.setView(latlng, this.mymap.getZoom());
        }

        let markers = this.markers[connectionId];
        if (markers) {
            // Existing marker

            markers.aircraft.setLatLng(latlng);
            markers.info.setLatLng(latlng);

            let className = 'divicon-aircraft-info';
            if (isMe) className += " me";

            let iconSizeValue: L.PointExpression = [iconSize, 60];

            if (isMoreInfo) {
                let htmlBody = `<div>${aircraftStatus.callsign}<br />ALT ${Math.round(aircraftStatus.altitude)} ft<br />HDG ${Math.round(aircraftStatus.heading)}\u00B0<br />IAS ${Math.round(aircraftStatus.indicatedAirSpeed)} kts</div>`

                if (aircraftStatus.trueHeading >= 180) {
                    markers.info.setIcon(L.divIcon({
                        className: className,
                        html: htmlBody,
                        iconSize: iconSizeValue,
                        iconAnchor: [-iconSize, 10],
                    }))
                } else {
                    markers.info.setIcon(L.divIcon({
                        className: className,
                        html: htmlBody,
                        iconSize: iconSizeValue,
                        iconAnchor: [infoBoxWidth + iconSize, 10],
                    }))
                }
            } else {
                if (aircraftStatus.trueHeading >= 180) {
                    markers.info.setIcon(L.divIcon({
                        className: className,
                        html: `<div>${aircraftStatus.callsign}</div>`,
                        iconSize: iconSizeValue,
                        iconAnchor: [-iconSize, 10],
                    }))
                } else {
                    markers.info.setIcon(L.divIcon({
                        className: className + ' right',
                        html: `<div>${aircraftStatus.callsign}</div>`,
                        iconSize: iconSizeValue,
                        iconAnchor: [infoBoxWidth + iconSize, 10],
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
                    iconSize: [iconSize, 50],
                    iconAnchor: [-iconSize, -4],
                }),
                zIndexOffset: 1000
            }).addTo(this.mymap)
            markers = {
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

        if (this.circleMarker && isMe) {
            this.circleMarker.setLatLng(latlng);
        }
    }

    public drawAirports(airports: Airport[]) {
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

    public drawFlightPlans(flightPlans: FlightPlanData[]) {
        if (this.mymap) {
            this.flightPlanLayerGroup.clearLayers();

            let index = 0;
            const colors = ['red', 'blue'];

            for (var flightPlan of flightPlans) {
                const latlngs = flightPlan.waypoints.reduce((prev: L.LatLngTuple[], curr) =>
                    prev.concat([[curr.latitude, curr.longitude]]),
                    [])

                const polyline = L.polyline(latlngs, { color: colors[(index++ % colors.length)] });
                this.flightPlanLayerGroup.addLayer(polyline);

                for (let waypoint of flightPlan.waypoints) {
                    const marker = L.marker([waypoint.latitude, waypoint.longitude], {
                        title: waypoint.id,
                        icon: L.divIcon({
                            className: 'divicon-waypoint',
                            html: `<div style="width: 8px; height: 8px; background-color: black; border-radius: 4px"></div><div>${waypoint.id}</div>`,
                            iconSize: [8, 8],
                            iconAnchor: [4, 4],
                        })

                    });
                    this.flightPlanLayerGroup.addLayer(marker);
                }
            }
        }
    }

    public focusAircraft(aircraftStatus: AircraftStatus) {
        if (this.mymap) {
            let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];
            this.mymap.setView(latlng, this.mymap.getZoom());
        }
    }

    public cleanUp(connectionId: string, isMe: boolean) {
        if (this.mymap !== undefined) {
            const marker = this.markers[connectionId];
            if (marker) {
                delete this.markers[connectionId];

                marker.aircraft.removeFrom(this.mymap);
                marker.info.removeFrom(this.mymap);
                if (isMe) {
                    this.removeRangeCircle();
                }
            }
        }
    }

    public addRangeCircle() {
        if (!this.circleMarker) {
            this.circleMarker = L.circle([0, 0], {
                radius: 5029,
                opacity: 0.5,
                fillOpacity: 0,
                weight: 2,
                color: `black`
            }).addTo(this.mymap);
        }
    }

    public removeRangeCircle() {
        if (this.circleMarker) {
            this.circleMarker.removeFrom(this.mymap);
            this.circleMarker = null;
        }
    }

    public onViewChanged(handler: OnViewChangedFn) {
        this.onViewChangedHandler = handler;
    }
}