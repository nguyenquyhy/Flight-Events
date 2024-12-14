import { IMap, MapTileType, OnViewChangedFn, View, OnTeleportPositionSelectedFn, OnSetClientIdFn, OnSetOptionalClientIdFn } from './IMap';
import * as L from 'leaflet';
import LG from 'leaflet-geometryutil'
import 'leaflet-rotatedmarker';
import 'overpass-layer';
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
    onTeleportPositionSelectedHandler: (OnTeleportPositionSelectedFn | null) = null;
    onSetMeHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetFollowHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetShowPlanHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetShowInfoHandler: (OnSetClientIdFn | null) = null;
    onSetShowRouteHandler: (OnSetClientIdFn | null) = null;

    initialView?: View;
    lastView?: View;

    isDark: boolean = false;
    isDrawing = false;
    isTeleporting = false;

    measurement: L.Polyline | null = null;

    public initialize(divId: string, view: View | undefined, mode: string | null) {
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
                .setView([view?.latitude || 51.505, view?.longitude || -0.09], view?.zoom || 13);
        this.initialView = this.lastView = view;

        L.control.attribution({
            position: 'bottomleft'
        }).addTo(map);
        if (mode === 'none') map.removeControl(map.zoomControl);

        this.mymap.on('moveend', (e) => {
            const zoom = map.getZoom();
            const center = map.getCenter();

            // HACK: due to this issue https://github.com/Leaflet/Leaflet/issues/3796, getCenter does not always work
            // We cache the last view for now
            this.lastView = { latitude: center.lat, longitude: center.lng, zoom: zoom };

            if (this.onViewChangedHandler) {
                this.onViewChangedHandler(this.lastView);
            }
        });

        this.mymap.on('mousedown', (e: L.LeafletMouseEvent) => {
            if (this.mymap && this.isDrawing) {
                this.measurement = L.polyline([e.latlng, e.latlng]).addTo(this.mymap);
            }
        });
        this.mymap.on('mousemove', (e: L.LeafletMouseEvent) => {
            if (this.isDrawing && this.measurement) {
                let latlng = this.measurement.getLatLngs() as L.LatLng[];
                latlng[1] = e.latlng;
                this.measurement.setLatLngs(latlng);
            }
        });
        this.mymap.on('mouseup', (e: L.LeafletMouseEvent) => {
            if (this.isDrawing) {
                this.stopDrawing();
            } else if (this.isTeleporting) {
                this.moveAircraft(e);
                this.isTeleporting = false;
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
                    stroke: true,
                    width: 1
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

        this.drawOshkoshCircles();
    }

    public deinitialize() {
        this.onViewChangedHandler = null;
        this.mymap?.remove();
    }

    public startTeleporting() {
        const map = this.mymap;
        if (map) {
            map.dragging.disable();
            map.getContainer().classList.add("crosshair");
            this.isTeleporting = true;
        }
    }

    public startDrawing() {
        const map = this.mymap;
        if (map) {
            map.dragging.disable();
            map.getContainer().classList.add("crosshair");
            this.measurement = null;
            this.isDrawing = true;
        }
    }

    public stopDrawing() {
        const map = this.mymap;
        if (map && this.isDrawing && this.measurement) {
            map.getContainer().classList.remove("crosshair");
            map.dragging.enable();
            this.isDrawing = false;

            const latlng = this.measurement.getLatLngs() as L.LatLng[];
            const nm = map.distance(latlng[0], latlng[1]) / 1852;
            const bearing = LG.bearing(latlng[0], latlng[1]);

            L.marker([(latlng[0].lat + latlng[1].lat) / 2, (latlng[0].lng + latlng[1].lng) / 2], {
                icon: L.divIcon({ html: `${nm.toFixed(1)}nm<br />${((bearing + 360) % 360).toFixed(0)}°`, className: 'measurement-label' })
            }).addTo(map);
        }
    }

    public changeMode(dark: boolean) {
        this.isDark = dark;
    }

    public setTileLayer(type: MapTileType) {
        this.baseLayerGroup.clearLayers();

        const tileSize = !!this.initialView?.scaling ? this.initialView.scaling * 256 : undefined;
        const zoomOffset = !!this.initialView?.scaling ? -(this.initialView.scaling - 1) : undefined;
        const additionalAttributes = (tileSize && zoomOffset) ? { tileSize, zoomOffset } : {};

        switch (type) {
            case MapTileType.OpenStreetMap:
                this.baseLayerGroup.addLayer(L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                    ...additionalAttributes
                }));
                break;
            case MapTileType.OpenTopoMap:
                this.baseLayerGroup.addLayer(L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
                    maxZoom: 17,
                    attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
                    ...additionalAttributes
                }));
                break;
            case MapTileType.EsriWorldImagery:
                this.baseLayerGroup.addLayer(L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
                    attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community'
                }));
                break;
            case MapTileType.EsriTopo:
                this.baseLayerGroup.addLayer(L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}', {
                    attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ, TomTom, Intermap, iPC, USGS, FAO, NPS, NRCAN, GeoBase, Kadaster NL, Ordnance Survey, Esri Japan, METI, Esri China (Hong Kong), and the GIS User Community',
                    ...additionalAttributes
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

    public moveATCMarker(clientId: string, status: ATCStatus | null, info: ATCInfo | null) {
        if (status && info && this.mymap) {
            const latlng: L.LatLngExpression = [status.latitude, status.longitude];

            const marker = this.atcMarkers[clientId];
            if (marker) {
                // Existing marker
                marker
                    .bindPopup(`<strong>${info.callsign} [${(status.frequencyCom / 1000)}]</strong><br />Name: ${info.realName}<br />Certificate: ${info.certificate}`)
                    .setLatLng(latlng);
            } else {
                this.atcMarkers[clientId] = L.marker(latlng, {
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
            this.cleanUpController(clientId);
        }
    }

    public moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isShowingPlan: boolean, isMoreInfo: boolean, isShowingRoute: boolean) {
        if (!this.mymap) return;

        const iconSize = 12
        const infoBoxWidth = 100

        let latlng: L.LatLngExpression = [aircraftStatus.latitude, aircraftStatus.longitude];

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

        const popupDiv = L.DomUtil.create('div');
        let popup = `Altitude: ${Math.floor(aircraftStatus.altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.indicatedAirSpeed)}<br />`;
        if (aircraftStatus.callsign) {
            popup = `<b>${aircraftStatus.callsign}</b><br />${popup}`;
        }
        popupDiv.innerHTML = popup;

        const popupBtnGroup = L.DomUtil.create('div', 'btn-group-vertical');
        popupDiv.appendChild(popupBtnGroup);

        const setMeButton = this.createToggleButton(popupBtnGroup, 'My aircraft', isMe);
        const setFollowButton = this.createToggleButton(popupBtnGroup, 'Follow', isFollowing);
        const setFlightPlanButton = this.createToggleButton(popupBtnGroup, 'Show Plan', isShowingPlan);
        const setShowInfoButton = this.createToggleButton(popupBtnGroup, 'Show Info', isMoreInfo);
        const setShowRouteButton = this.createToggleButton(popupBtnGroup, 'Show Route', isShowingRoute);

        L.DomEvent.on(setMeButton, 'click', () => { this.onSetMeHandler && this.onSetMeHandler(isMe ? null : connectionId); this.mymap?.closePopup(); });
        L.DomEvent.on(setFollowButton, 'click', () => { this.onSetFollowHandler && this.onSetFollowHandler(isFollowing ? null : connectionId); this.mymap?.closePopup(); });
        L.DomEvent.on(setFlightPlanButton, 'click', () => { this.onSetShowPlanHandler && this.onSetShowPlanHandler(isShowingPlan ? null : connectionId); this.mymap?.closePopup(); });
        L.DomEvent.on(setShowInfoButton, 'click', () => { this.onSetShowInfoHandler && this.onSetShowInfoHandler(connectionId); this.mymap?.closePopup(); });
        L.DomEvent.on(setShowRouteButton, 'click', () => { this.onSetShowRouteHandler && this.onSetShowRouteHandler(connectionId); this.mymap?.closePopup(); });

        markers.info.bindPopup(popupDiv, {
            autoPan: false
        });
        markers.info.setIcon(markers.info.getIcon());
        markers.aircraft
            .bindPopup(popupDiv)
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

                if (flightPlan.waypoints) {
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
                                html: !!waypoint.id ? `<div style="width: 8px; height: 8px; background-color: black; border-radius: 4px"></div><div>${waypoint.id}</div>` : '',
                                iconSize: [8, 8],
                                iconAnchor: [4, 4],
                            })

                        });
                        this.flightPlanLayerGroup.addLayer(marker);
                    }
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

    public focus(location: { longitude: number, latitude: number }, zoom?: number) {
        if (this.mymap) {
            let latlng: L.LatLngExpression = [location.latitude, location.longitude];
            this.mymap.setView(latlng, zoom || this.mymap.getZoom());
        }
    }

    public cleanUpController(clientId: string) {
        const marker = this.atcMarkers[clientId];
        if (marker) {
            marker.remove();
            delete this.atcMarkers[clientId];
        }
    }

    public cleanUpAircraft(clientId: string, isMe: boolean) {
        const marker = this.markers[clientId];
        if (marker) {
            delete this.markers[clientId];

            marker.aircraft.remove();
            marker.info.remove();
            if (isMe) {
                this.removeRangeCircle();
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

    public getCurrentView(): View {
        // HACK: due to https://github.com/Leaflet/Leaflet/issues/3796, getCenter() doesn't work here
        return this.lastView || { longitude: null, latitude: null };
    }

    public onViewChanged(handler: OnViewChangedFn) {
        this.onViewChangedHandler = handler;
    }

    public onTeleportPositionSelected(handler: OnTeleportPositionSelectedFn) {
        this.onTeleportPositionSelectedHandler = handler;
    }

    public onSetMe(handler: OnSetOptionalClientIdFn) {
        this.onSetMeHandler = handler;
    }
    public onSetFollow(handler: OnSetOptionalClientIdFn) {
        this.onSetFollowHandler = handler;
    }
    public onSetShowPlan(handler: OnSetOptionalClientIdFn) {
        this.onSetShowPlanHandler = handler;
    }
    public onSetShowInfo(handler: OnSetClientIdFn) {
        this.onSetShowInfoHandler = handler;
    }
    public onSetShowRoute(handler: OnSetClientIdFn) {
        this.onSetShowRouteHandler = handler;
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

    private moveAircraft(e: { latlng: L.LatLng }) {
        if (this.onTeleportPositionSelectedHandler) {
            this.onTeleportPositionSelectedHandler({ latitude: e.latlng.lat, longitude: e.latlng.lng });
        }
    }

    private createToggleButton(container: HTMLElement, content: string, isToggled: boolean) {
        const button = L.DomUtil.create('button', 'btn btn-sm btn-info' + (isToggled ? ' active' : ''), container);
        button.innerHTML = content;
        button.setAttribute('data-toggle', 'button');
        if (isToggled) button.setAttribute('aria-pressed', 'true');
        container.appendChild(button);
        return button;
    }

    private drawOshkoshCircles() {
        if (this.mymap) {
            const commonStyle = { fill: true, fillOpacity: 1, radius: 12, stroke: false };
            L.circle([43.98127188305071, -88.55649845579117], { ...commonStyle, fillColor: "blue" }).addTo(this.mymap);
            L.circle([43.97694160803118, -88.55654726863388], { ...commonStyle, fillColor: "red" }).addTo(this.mymap);
            L.circle([43.972867792605214, -88.55660272193413], { ...commonStyle, fillColor: "yellow" }).addTo(this.mymap);
            L.circle([43.968619800959615, -88.5566557100092], { ...commonStyle, fillColor: "purple" }).addTo(this.mymap);

            L.circle([43.991529704426085, -88.5545677074355], { ...commonStyle, fillColor: "orange" }).addTo(this.mymap);
            L.circle([43.991566231901906, -88.56045717742607], { ...commonStyle, fillColor: "cyan" }).addTo(this.mymap);
            L.circle([43.99160231883673, -88.56532600272514], { ...commonStyle, fillColor: "white" }).addTo(this.mymap);

            const latDiff = 0.00006;
            const lngDiff = 0.00008;
            L.rectangle([[43.96952516618286 - latDiff, -88.55455903934717 - lngDiff], [43.96952516618286 + latDiff, -88.55455903934717 + lngDiff]], { ...commonStyle, fillColor: "cyan" }).addTo(this.mymap);
            L.rectangle([[43.97348742641145 - latDiff, -88.55450902110248 - lngDiff], [43.97348742641145 + latDiff, -88.55450902110248 + lngDiff]], { ...commonStyle, fillColor: "red" }).addTo(this.mymap);
        }
    }
}
