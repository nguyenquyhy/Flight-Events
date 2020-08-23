import { IMap, MapTileType, OnViewChangedFn, View, OnAircraftMovedFn } from './IMap';
import * as L from 'leaflet';
import 'leaflet-rotatedmarker';
import 'overpass-layer/dist/overpass-layer';
import 'leaflet-contextmenu';
import 'leaflet-contextmenu/dist/leaflet.contextmenu.css';
import { AircraftStatus, Airport, FlightPlanData, ATCStatus, ATCInfo, AircraftStatusBrief } from '../Models';


interface Markers {
    aircraft: L.Marker<any>
    info: L.Marker<any>
}

const ROUTE_AIR_COLOR = 'blue';
const ROUTE_GROUND_COLOR = 'brown';

// Augment types of leaflet-contextmenu
declare module 'leaflet' {
    interface MapOptions {
        contextmenu: boolean;
        contextmenuWidth: number;
        contextmenuItems: (string | ContextMenuItem)[]
    }

    interface ContextMenuItem {
        text: string;
        icon?: string;
        callback?: (ContextMenuEventArgs) => void;
    }

    interface ContextMenuEventArgs {
        containerPoint: L.Point;
        latlng: L.LatLng;
        layerPoint: L.Point;
    }
}

export default class LeafletMap implements IMap {
    mymap?: L.Map;

    baseLayerGroup: L.LayerGroup = L.layerGroup();
    airportLayerGroup: L.LayerGroup = L.layerGroup();
    airportMarkers: { [indent: string]: L.Marker } = {};

    markers: { [connectionId: string]: Markers } = {};
    atcMarkers: { [connectionId: string]: L.Marker } = {};

    flightPlanLayerGroup: L.LayerGroup = L.layerGroup();

    routeLayerGroups: { [id: string]: L.LayerGroup } = {};
    routeLines: { [id: string]: L.Polyline } = {};
    trackingStatuses: { [id: string]: AircraftStatusBrief } = {};

    circleMarker?: L.Circle;

    onViewChangedHandler: (OnViewChangedFn | null) = null;
    onAircraftMovedHandler: (OnAircraftMovedFn | null) = null;

    isDark: boolean = false;

    public initialize(divId: string, view?: View) {
        const map = this.mymap =
            L.map(divId, {
                attributionControl: false,
                contextmenu: true,
                contextmenuWidth: 140,
                contextmenuItems: [{
                    text: 'Teleport aircraft here',
                    callback: this.moveAircraft.bind(this)
                }]
            })
                .setView(view ? [view.latitude, view.longitude] : [51.505, -0.09], view ? view.zoom : 13);
        L.control.attribution({
            position: 'bottomleft'
        }).addTo(map);

        this.mymap.on('moveend', (e) => {
            const zoom = map.getZoom();
            const center = map.getCenter();
            if (this.onViewChangedHandler) {
                this.onViewChangedHandler({ latitude: center.lat, longitude: center.lng, zoom: zoom });
            }
        });

        this.baseLayerGroup.addTo(this.mymap);

        //L.OverPassLayer({
        //    'query': '[out:json][timeout:25];(way["aeroway"="taxiway"]({{bbox}}););out body;>;out skel qt;'
        //}).addTo(this.mymap);
        var overpassFrontend = new (window as any).OverpassFrontend('//overpass-api.de/api/interpreter')

        new (window as any).OverpassLayer({
            query: '(way[aeroway=runway];)',
            minZoom: 14,
            overpassFrontend: overpassFrontend,
            feature: {
                markerSymbol: (o: any) => {
                    return o.tags.ref ? `<strong height="20" anchorY="10" style="font-size: 1.3em;background-color: rgba(255, 255, 255, 0.7);padding: 3px;border-radius: 3px">${o.tags.ref}</strong>` : null;
                },
                style: {
                    stroke: true
                }
            }
        }).addTo(this.mymap);
        new (window as any).OverpassLayer({
            query: '(way[aeroway=taxiway];way[aeroway=taxilane];way[aeroway=parking_position];)',
            minZoom: 15,
            overpassFrontend: overpassFrontend,
            feature: {
                markerSymbol: (o: any) => {
                    return o.tags.ref ? `<strong height="20" anchorY="10" style="background-color: rgba(255, 255, 255, 0.7);padding: 3px;border-radius: 3px">${o.tags.ref}</strong>` : null;
                },
                style: {
                    width: 2,
                    color: '#0CBBC9'
                }
            }
        }).addTo(this.mymap);

        this.airportLayerGroup.addTo(this.mymap);
        this.flightPlanLayerGroup.addTo(this.mymap);

        setInterval(this.cleanUp, 2000);
    }

    public deinitialize() {
        this.onViewChangedHandler = null;
        this.mymap?.remove();
    }

    public changeMode(dark: boolean) {
        this.isDark = dark;
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
        if (status && info && this.mymap) {
            const latlng: L.LatLngExpression = [status.latitude, status.longitude];

            const marker = this.atcMarkers[connectionId];
            if (marker) {
                // Existing marker
                marker
                    .bindPopup(`<strong>${info.callsign} [${(status.frequencyCom / 1000)}]</strong><br />Name: ${info.realName}<br />Certificate: ${info.certificate}`)
                    .setLatLng(latlng);
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
        if (!this.mymap) return;

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
            if (this.isDark) className += " dark";

            let iconSizeValue: L.PointExpression = [iconSize, 60];

            if (isMoreInfo) {
                let htmlBody = `<div>${aircraftStatus.callsign}<br />ALT ${Math.round(aircraftStatus.altitude)} ft<br />HDG ${Math.round(aircraftStatus.heading)}\u00B0<br />IAS ${Math.round(aircraftStatus.indicatedAirSpeed)} kts<br />GS ${Math.round(aircraftStatus.groundSpeed)} kts</div>`

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
                    className: 'icon-aircraft-marker',
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

            let over180Line = false;

            for (var flightPlan of flightPlans) {
                let latlngArray: L.LatLngTuple[] = [];

                for (let i = 0; i < flightPlan.waypoints.length; i++) {
                    const waypoint = flightPlan.waypoints[i];
                    const latlng: L.LatLngTuple = [waypoint.latitude, waypoint.longitude];

                    if (i === 0 || 180 > Math.abs(waypoint.longitude - flightPlan.waypoints[i - 1].longitude)) {
                        latlngArray.push(latlng);
                    }
                    else {
                        // If the path take more than half the earth, draw the other half
                        over180Line = true;

                        const prevWaypoint = flightPlan.waypoints[i - 1];

                        let distance180 = 180 - prevWaypoint.longitude;
                        let differenceLng = (360 - prevWaypoint.longitude) + waypoint.longitude;
                        let line180 = 180;
                        if (prevWaypoint.longitude < 0) {
                            distance180 = -180 - prevWaypoint.longitude;
                            differenceLng = (-360 - prevWaypoint.longitude) + waypoint.longitude;
                            line180 = -180;
                        }

                        const differenceLat = ((waypoint.latitude + 90) - (prevWaypoint.latitude + 90)) / differenceLng;

                        latlngArray.push([prevWaypoint.latitude + differenceLat * distance180, line180]);

                        // Break new line
                        const polyline = L.polyline(latlngArray, { color: colors[(index % colors.length)] });
                        this.flightPlanLayerGroup.addLayer(polyline);

                        latlngArray = [
                            [prevWaypoint.latitude + differenceLat * distance180, -line180],
                            latlng
                        ];
                    }

                    if (i === flightPlan.waypoints.length - 1) {
                        // Last item
                        const polyline = L.polyline(latlngArray, { color: colors[(index % colors.length)] });
                        this.flightPlanLayerGroup.addLayer(polyline);
                    }
                }
                index++;
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

            if (over180Line) {
                // Draw some dotted line to indicate that the flight plan wrap around
                const latlngNegative180: L.LatLngTuple[] = [
                    [90, -180],
                    [-90, -180]
                ];
                const latlngPositive180: L.LatLngTuple[] = [
                    [90, 180],
                    [-90, 180]
                ];

                const polyline0 = L.polyline(latlngNegative180, { color: 'black', dashArray: '5, 10' });
                this.flightPlanLayerGroup.addLayer(polyline0);
                const polyline1 = L.polyline(latlngPositive180, { color: 'black', dashArray: '5, 10' });
                this.flightPlanLayerGroup.addLayer(polyline1);
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
        if (this.mymap && !this.circleMarker) {
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
        if (this.mymap && this.circleMarker) {
            this.circleMarker.removeFrom(this.mymap);
            this.circleMarker = undefined;
        }
    }

    public onViewChanged(handler: OnViewChangedFn) {
        this.onViewChangedHandler = handler;
    }

    public onAircraftMoved(handler: OnAircraftMovedFn) {
        this.onAircraftMovedHandler = handler;
    }

    public track(id: string, status: AircraftStatus) {
        if (!this.routeLines[id] || !this.trackingStatuses[id] || this.trackingStatuses[id].isOnGround !== status.isOnGround) {
            this.routeLines[id] = this.createRouteLine(id, this.trackingStatuses[id] ? [this.trackingStatuses[id]] : [], status.isOnGround);
        }
        this.trackingStatuses[id] = status;

        if (status.groundSpeed > 0.05) {
            this.routeLines[id].addLatLng([status.latitude, status.longitude]);
        }
    }

    public prependTrack(id: string, route: AircraftStatusBrief[]) {
        let isOnGround: boolean | null = null;
        let routeSoFar: AircraftStatusBrief[] = [];
        for (let status of route) {
            if (isOnGround !== status.isOnGround) {
                if (routeSoFar.length > 0 && isOnGround !== null) {
                    this.createRouteLine(id, routeSoFar, isOnGround);
                }
                routeSoFar = routeSoFar.length > 0 ? routeSoFar.slice(routeSoFar.length - 1, routeSoFar.length) : [];
                isOnGround = status.isOnGround;
            }
            routeSoFar.push(status);
        }
        if (routeSoFar.length > 0 && isOnGround !== null) {
            this.createRouteLine(id, routeSoFar, isOnGround);
        }
    }

    private createRouteLine(id: string, route: AircraftStatusBrief[], isOnGround: boolean) {
        if (this.mymap && !this.routeLayerGroups[id]) {
            this.routeLayerGroups[id] = L.layerGroup().addTo(this.mymap);
        }
        return L.polyline(route.map(r => [r.latitude, r.longitude]), { color: isOnGround ? ROUTE_GROUND_COLOR : ROUTE_AIR_COLOR }).addTo(this.routeLayerGroups[id]);
    }

    public clearTrack(id: string) {
        delete this.trackingStatuses[id];
        if (this.routeLayerGroups[id]) {
            this.routeLayerGroups[id].remove();
            delete this.routeLayerGroups[id];
        }
        delete this.routeLines[id];
    }

    private moveAircraft(e: L.ContextMenuEventArgs) {
        if (this.onAircraftMovedHandler) {
            this.onAircraftMovedHandler({ latitude: e.latlng.lat, longitude: e.latlng.lng });
        }
    }
}
