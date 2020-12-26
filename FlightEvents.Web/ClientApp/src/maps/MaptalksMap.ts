﻿import { IMap, MapTileType, OnViewChangedFn, View, OnAircraftMovedFn, OnSetClientIdFn, OnSetOptionalClientIdFn } from './IMap';
import * as maptalks from 'maptalks';
import { AircraftStatus, Airport, FlightPlanData, ATCStatus, ATCInfo, AircraftStatusBrief, FlightPlanWaypoint } from '../Models';

interface Markers {
    aircraft: Sector
    aircraftLine: Marker
    info: Label
}

interface Coordinate {
    x: number;
    y: number;
    sub: (other: Coordinate) => any
}

interface Point {

}

interface Map {
    panTo: (latlng: Coordinate) => void;
    setPitch: (pitch: number) => void;
    getBaseLayer: () => TileLayer;
    setBaseLayer: (layer: TileLayer) => void;
    remove: () => void;
    on: (events: string, handler: () => void) => void;
    getCenter: () => Coordinate;
    setCenter: (coordinate: Coordinate) => void;
    getZoom: () => number;
    setZoom: (zoom: number) => void;
    getMinZoom: () => number;
    getMaxZoom: () => number;
    fitExtent: (extent: any, zoomOffset: number, option?: any) => void;
    getProjection: () => any;
    coordinateToContainerPoint: (coord: Coordinate, zoom?: number, point?: Point) => Point;
    setMenu: (menu: any) => void;
}

interface Layer {
    addTo: (map: Map) => void;
}

interface TileLayer extends Layer {

}

interface VectorLayer extends Layer {
    addGeometry: (markers: Geometry | Geometry[]) => void;
    clear: () => void;
    remove: () => void;
}

interface LineString extends Geometry {
    getCoordinates: () => Coordinate[];
    setCoordinates: (coordinates: Coordinate[]) => void;
}

interface Geometry {
    show: () => void;
    hide: () => void;
    remove: () => void;
    getProperties: () => any;
    setProperties: (props: any) => void;
    updateSymbol: (props: any) => void;
    animate: (animation: any, props: any) => void;
    addTo: (layer: VectorLayer) => void;
    setInfoWindow: (infoWindow: any) => void;
    getInfoWindow: () => any;
}

interface Marker extends Geometry {
    setCoordinates: (coordinate: Coordinate) => Marker;
    getCoordinates: () => Coordinate;
    setInfoWindow: (info: any) => Marker;
}

interface Label extends Marker {
    setContent: (content: string) => void;
    getBoxStyle: () => any;
    setBoxStyle: (style: any) => void;
    getTextSymbol: () => any;
    setTextSymbol: (symbol: any) => void;
}

interface Sector extends Geometry {
    setStartAngle: (angle: number) => void;
    setEndAngle: (angle: number) => void;
    setRadius: (radius: number) => void;
    getCoordinates: () => Coordinate;
}

interface Circle extends Geometry {
    getCoordinates: () => Coordinate;
    setCoordinates: (corrdinates: Coordinate) => void;
}

const ROUTE_AIR_COLOR = 'blue';
const ROUTE_GROUND_COLOR = 'brown';

export default class MaptalksMap implements IMap {
    static AIRCRAFT_SIZE = 120;
    static ANIMATION_DURATION = 500;
    static FEET_TO_METER = .3048;

    map: Map | undefined;
    markers: { [connectionId: string]: Markers } = {};
    atcMarkers: { [connectionId: string]: Marker } = {};

    onViewChangedHandler: OnViewChangedFn | null = null;
    onAircraftMovedHandler: (OnAircraftMovedFn | null) = null;
    onSetMeHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetFollowHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetShowPlanHandler: (OnSetOptionalClientIdFn | null) = null;
    onSetShowInfoHandler: (OnSetClientIdFn | null) = null;
    onSetShowRouteHandler: (OnSetClientIdFn | null) = null;

    visibleCircle: Circle = new maptalks.Circle([0, 0], 5029, {
        symbol: {
            lineColor: '#000000',
            lineWidth: 2,
            lineOpacity: 0.25,
            polygonFill: 'none',
            polygonOpacity: 0,
        }
    })

    atcLayer: VectorLayer = new maptalks.VectorLayer('atc');
    aircraftLayer: VectorLayer | undefined;
    airportLayer: VectorLayer = new maptalks.VectorLayer('airport');
    flightPlanLayer: VectorLayer = new maptalks.VectorLayer('flightPlan', {
        enableAltitude: true,
        drawAltitude: {
            polygonFill: '#1bbc9b',
            polygonOpacity: 0.3,
            lineWidth: 0
        }
    });
    routeLayers: { [id: string]: VectorLayer } = {};
    routeLines: { [id: string]: LineString } = {};
    trackingStatuses: { [id: string]: AircraftStatusBrief } = {};

    isDark: boolean = false;
    type: MapTileType = MapTileType.OpenStreetMap;

    initialView?: View;

    initialize(divId: string, view: View | undefined, mode: string | null) {
        const map: Map = new maptalks.Map(divId, {
            center: view ? [view.longitude, view.latitude] : [-0.09, 51.505],
            zoom: view ? view.zoom : 13,
            pitch: 30
        });
        this.initialView = view;

        var options = {
            'items': [
                { item: 'Teleport aircraft here', click: this.moveAircraft.bind(this) }
            ]
        };
        map.setMenu(options)

        map.on('zoomend', () => this.handleZoom());
        map.on('moveend', () => {
            const zoom = map.getZoom();
            const center = map.getCenter();
            if (this.onViewChangedHandler) {
                this.onViewChangedHandler({ latitude: center.y, longitude: center.x, zoom: zoom });
            }
        });

        const aircraftLayer = new maptalks.VectorLayer('aircraft', {
            enableAltitude: true,
            // draw altitude
            drawAltitude: {
                lineWidth: 1,
                lineColor: '#aaa'
            }
        }).addTo(map);

        aircraftLayer.addGeometry(this.visibleCircle);

        this.atcLayer.addTo(map);
        this.airportLayer.addTo(map);
        this.flightPlanLayer.addTo(map);

        this.visibleCircle.hide();

        this.aircraftLayer = aircraftLayer;
        this.map = map;
    }

    deinitialize() {
        this.onViewChangedHandler = null;
        if (this.map) {
            this.map.remove();
        }
    }

    changeMode(dark: boolean) {
        this.isDark = dark;
        this.setTileLayer(this.type);
    }

    setTileLayer(type: MapTileType) {
        if (!this.map) return;

        this.type = type;
        const cssFilter = this.isDark ? 'hue-rotate(180deg) invert(100%)' : '';

        switch (type) {
            case MapTileType.OpenStreetMap:
                this.map.setBaseLayer(new maptalks.TileLayer('openstreetmap', {
                    urlTemplate: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c'],
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                    cssFilter: cssFilter
                }));
                break;
            case MapTileType.OpenTopoMap:
                this.map.setBaseLayer(new maptalks.TileLayer('opentopomap', {
                    urlTemplate: 'https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c'],
                    maxZoom: 17,
                    attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
                    cssFilter: cssFilter
                }));
                break;
            case MapTileType.EsriWorldImagery:
                this.map.setBaseLayer(new maptalks.TileLayer('esri', {
                    urlTemplate: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
                    attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community',
                    cssFilter: cssFilter
                }));
                break;
            case MapTileType.EsriTopo:
                this.map.setBaseLayer(new maptalks.TileLayer('esritopo', {
                    urlTemplate: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}',
                    attribution: 'Tiles &copy; Esri &mdash; Esri, DeLorme, NAVTEQ, TomTom, Intermap, iPC, USGS, FAO, NPS, NRCAN, GeoBase, Kadaster NL, Ordnance Survey, Esri Japan, METI, Esri China (Hong Kong), and the GIS User Community',
                    cssFilter: cssFilter
                }));
                break;
            case MapTileType.Carto:
                this.map.setBaseLayer(new maptalks.TileLayer('carto', {
                    urlTemplate: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png',
                    subdomains: ['a', 'b', 'c', 'd'],
                    attribution: '&copy; <a href="http://osm.org">OpenStreetMap</a> contributors, &copy; <a href="https://carto.com/">CARTO</a>',
                    cssFilter: cssFilter
                }));
                break;
            case MapTileType.UsVfrSectional:
                this.map.setBaseLayer(new maptalks.TileLayer('usvfrsection', {
                    urlTemplate: 'https://wms.chartbundle.com/tms/v1.0/sec/{z}/{x}/{y}.png?type=google',
                    attribution: 'Map data: &copy; Federal Aviation Administration (FAA), <a href="http://chartbundle.com">ChartBundle.com</a>',
                    cssFilter: cssFilter
                }));
                break;
        }
    }

    moveATCMarker(clientId: string, status: ATCStatus | null, info: ATCInfo | null) {
        if (status && info) {
            const latlng: Coordinate = new maptalks.Coordinate([status.longitude, status.latitude]);

            const marker = this.atcMarkers[clientId];
            if (marker) {
                // Existing marker
                marker
                    .setInfoWindow({
                        title: `${info.callsign} [${(status.frequencyCom / 1000)}]`,
                        content: `Name: ${info.realName}<br />Certificate: ${info.certificate}`,
                        autoCloseOn: 'click'
                    })
                    .setCoordinates(latlng);
            } else {
                this.atcMarkers[clientId] = new maptalks.Marker(latlng, {
                    symbol: {
                        markerFile: 'marker-tower.png',
                        markerWidth: 30,
                        markerHeight: 30
                    }
                })
                    .addTo(this.atcLayer)
                    .setInfoWindow({
                        title: `${info.callsign} [${(status.frequencyCom / 1000)}]`,
                        content: `Name: ${info.realName}<br />Certificate: ${info.certificate}`,
                        autoCloseOn: 'click'
                    });
            }
        } else {
            // Remove
            this.cleanUpController(clientId);
        }
    }

    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isShowingPlan: boolean, isMoreInfo: boolean, isShowingRoute: boolean) {
        if (!this.map || !this.aircraftLayer) return;

        let latlng: Coordinate = new maptalks.Coordinate([aircraftStatus.longitude, aircraftStatus.latitude]);

        const altitude = aircraftStatus.altitude * MaptalksMap.FEET_TO_METER

        let markers = this.markers[connectionId];
        if (markers) {
            // Existing marker

            const offset = latlng.sub(markers.aircraft.getCoordinates());

            markers.aircraft.setProperties({ altitude: altitude });
            markers.aircraft.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });
            markers.aircraft.setStartAngle(-aircraftStatus.trueHeading - 10 - 90);
            markers.aircraft.setEndAngle(-aircraftStatus.trueHeading + 10 - 90);

            markers.aircraftLine.setProperties({ altitude: altitude });
            markers.aircraftLine.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });

            markers.info.setProperties({ altitude: altitude });
            markers.info.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });

            // Change theme for dark mode
            markers.info.setBoxStyle({
                ...markers.info.getBoxStyle(),
                symbol: {
                    markerType: 'square',
                    markerFill: this.isDark ? 'rgba(0, 0, 0)' : 'rgba(255, 255, 255)',
                    markerFillOpacity: 0.2,
                    markerLineWidth: 0
                }
            })
            markers.info.setTextSymbol({
                ...markers.info.getTextSymbol(),
                textFill: this.isDark ? '#ddd' : '#34495e',
            })

            if (isMe) {
                const circleOffset = latlng.sub(this.visibleCircle.getCoordinates());
                this.visibleCircle.setProperties({ altitude: altitude });
                this.visibleCircle.animate({ translate: [circleOffset['x'], circleOffset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });
            }

            if (isMe) {
                markers.aircraft.updateSymbol({
                    lineColor: this.isDark ? 'gold' : 'rgb(135,196,240)',
                    polygonFill: this.isDark ? 'gold' : 'rgb(135,196,240)'
                })
            } else {
                markers.aircraft.updateSymbol({
                    lineColor: this.isDark ? '#ddd' : '#34495e',
                    polygonFill: this.isDark ? '#ddd' : '#34495e'
                })
            }

            const body = isMoreInfo ?
                `${aircraftStatus.callsign}\nALT ${Math.round(aircraftStatus.altitude)} ft\nHDG ${Math.round(aircraftStatus.heading)}\u00B0\nIAS ${Math.round(aircraftStatus.indicatedAirSpeed)} kts\nGS ${Math.round(aircraftStatus.groundSpeed)} kts` :
                `${aircraftStatus.callsign}`
            markers.info.setContent(body);
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

                    'markerRotation': -aircraftStatus.trueHeading
                },
                properties: {
                    altitude: altitude
                },
            })
            const aircraft = new maptalks.Sector(latlng, this.determineAircraftSize(), -aircraftStatus.trueHeading - 10 - 90, -aircraftStatus.trueHeading + 10 - 90, {
                symbol: {
                    lineColor: '#34495e',
                    lineWidth: 2,
                    polygonFill: '#34495e',
                    polygonOpacity: 1
                },
                properties: {
                    altitude: altitude
                }
            })
            const info = new maptalks.Label(`${aircraftStatus.callsign}`, latlng, {
                textSymbol: {
                    textFaceName: 'Lucida Console',
                    textFill: '#34495e',
                    textSize: 12,
                    textWeight: 'bold',
                    textVerticalAlignment: 'top',
                    textHorizontalAlignment: 'right',
                    textDy: -10,
                },
                boxStyle: {
                    padding: [4, 4],
                    verticalAlignment: 'top',
                    horizontalAlignment: 'right',
                    minWidth: 50,
                    minHeight: 20,
                    symbol: {
                        markerType: 'square',
                        markerFill: 'rgba(255, 255, 255)',
                        markerFillOpacity: 0.2,
                        markerLineWidth: 0
                    }
                },
                properties: {
                    altitude: altitude
                }
            });
            markers = {
                aircraft: aircraft,
                aircraftLine: aircraftLine,
                info: info
            }
            this.markers[connectionId] = markers;

            this.aircraftLayer.addGeometry([].concat(aircraft, aircraftLine, info));

            if (isMe) {
                this.visibleCircle.setCoordinates(latlng);
            }
        }

        this.createInfoWindow(markers, aircraftStatus, isMe, isFollowing, isShowingPlan, isMoreInfo, isShowingRoute, connectionId);
    }

    private createInfoWindow(markers: Markers, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isShowingPlan: boolean, isMoreInfo: boolean, isShowingRoute: boolean, connectionId: string) {
        let popup = `Altitude: ${Math.floor(aircraftStatus.altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.indicatedAirSpeed)}<br />`;
        if (aircraftStatus.callsign) {
            popup = `<b>${aircraftStatus.callsign}</b><br />${popup}`;
        }
        let popupElement = document.createElement("div");
        popupElement.innerHTML = popup;

        const popupBtnGroup = document.createElement('div');
        popupBtnGroup.className = 'btn-group-vertical';
        popupElement.appendChild(popupBtnGroup);

        const setMeButton = this.createToggleButton(popupBtnGroup, 'My aircraft', isMe);
        const setFollowButton = this.createToggleButton(popupBtnGroup, 'Follow', isFollowing);
        const setFlightPlanButton = this.createToggleButton(popupBtnGroup, 'Show Plan', isShowingPlan);
        const setShowInfoButton = this.createToggleButton(popupBtnGroup, 'Show Info', isMoreInfo);
        const setShowRouteButton = this.createToggleButton(popupBtnGroup, 'Show Route', isShowingRoute);

        let infoWindow = markers.aircraft.getInfoWindow();
        if (infoWindow) {
            infoWindow.setContent(popupElement);
        } else {
            infoWindow = new maptalks.ui.InfoWindow({
                'content': popupElement,
                'autoCloseOn': 'click',
                'animationOnHide': false,
                width: 115
            });
            markers.aircraft.setInfoWindow(infoWindow);
            markers.info.setInfoWindow(infoWindow);
        }

        setMeButton.addEventListener('click', () => { this.onSetMeHandler && this.onSetMeHandler(isMe ? null : connectionId); infoWindow.hide(); });
        setFollowButton.addEventListener('click', () => { this.onSetFollowHandler && this.onSetFollowHandler(isFollowing ? null : connectionId); infoWindow.hide(); });
        setFlightPlanButton.addEventListener('click', () => { this.onSetShowPlanHandler && this.onSetShowPlanHandler(isShowingPlan ? null : connectionId); infoWindow.hide(); });
        setShowInfoButton.addEventListener('click', () => { this.onSetShowInfoHandler && this.onSetShowInfoHandler(connectionId); infoWindow.hide(); });
        setShowRouteButton.addEventListener('click', () => { this.onSetShowRouteHandler && this.onSetShowRouteHandler(connectionId); infoWindow.hide(); });
    }

    private createToggleButton(container: HTMLElement, content: string, isToggled: boolean) {
        const button = document.createElement('button');
        button.className = 'btn btn-sm btn-info' + (isToggled ? ' active' : '');
        button.innerHTML = content;
        button.setAttribute('data-toggle', 'button');
        if (isToggled) button.setAttribute('aria-pressed', 'true');
        container.appendChild(button);
        return button;
    }

    drawAirports(airports: Airport[]) {
        if (this.map) {
            const minLongitude = airports.reduce((prev, curr) => Math.min(prev, curr.longitude), 180);
            const maxLongitude = airports.reduce((prev, curr) => Math.max(prev, curr.longitude), -180);
            const minLatitude = airports.reduce((prev, curr) => Math.min(prev, curr.latitude), 90);
            const maxLatitude = airports.reduce((prev, curr) => Math.max(prev, curr.latitude), -90);

            this.fitExtent(new maptalks.Coordinate(minLongitude, minLatitude), new maptalks.Coordinate(maxLongitude, maxLatitude));

            this.airportLayer.clear();

            for (let airport of airports) {
                new maptalks.ui.UIMarker(
                    new maptalks.Coordinate([airport.longitude, airport.latitude]),
                    {
                        content: `<div><strong>${airport.ident}</strong></div>`,
                        dy: -40
                    }
                ).addTo(this.airportLayer).show();

                new maptalks.Marker(
                    new maptalks.Coordinate([airport.longitude, airport.latitude])
                ).addTo(this.airportLayer);
            }
        }
    }

    drawFlightPlans(flightPlans: FlightPlanData[]) {
        if (this.map) {
            this.flightPlanLayer.clear();

            let index = 0;
            const colors = ['red', 'blue'];

            for (let flightPlan of flightPlans) {
                const latlngs = flightPlan.waypoints.reduce((prev: Coordinate[], curr) =>
                    prev.concat(new maptalks.Coordinate([curr.longitude, curr.latitude])),
                    [])

                const altitudes = flightPlan.waypoints.reduce((prev: number[], curr, index) =>
                    prev.concat(MaptalksMap.FEET_TO_METER *
                        (curr.altitude ||
                            (index === 0 || index === flightPlan.waypoints.length - 1 ? 0 : flightPlan.cruisingAltitude))),
                    [])

                new maptalks.LineString(latlngs, {
                    symbol: {
                        lineColor: colors[(index++ % colors.length)],
                        lineWidth: 3
                    },
                    properties: {
                        altitude: altitudes
                    }
                }).addTo(this.flightPlanLayer);

                for (let i = 0; i < flightPlan.waypoints.length; i++) {
                    const waypoint = flightPlan.waypoints[i];
                    new maptalks.Marker(new maptalks.Coordinate([waypoint.longitude, waypoint.latitude]), {
                        properties: {
                            name: waypoint.id,
                            altitude: altitudes[i],
                        },
                        symbol: {
                            textFaceName: 'Lucida Console',
                            textName: '{name}',
                            textWeight: 'bold',
                            textDy: -15,
                        }
                    }).addTo(this.flightPlanLayer);
                }
            }
        }
    }

    public drawCheckpoints(checkpoints: FlightPlanWaypoint[]) {

    }

    focus(location: { longitude: number, latitude: number }) {
        if (this.map) {
            this.map.panTo(new maptalks.Coordinate([location.longitude, location.latitude]));
        }
    }

    cleanUpController(clientId: string) {
        const marker = this.atcMarkers[clientId];
        if (marker) {
            marker.remove();
            delete this.atcMarkers[clientId];
        }
    }

    cleanUpAircraft(clientId: string, isMe: boolean) {
        const marker = this.markers[clientId];
        if (marker) {
            delete this.markers[clientId];

            marker.aircraft.remove();
            marker.aircraftLine.remove();
            marker.info.remove();
            if (isMe) {
                this.removeRangeCircle();
            }
        }
    }

    addRangeCircle() {
        this.visibleCircle.show();
    }

    removeRangeCircle() {
        this.visibleCircle.hide();
    }

    public onViewChanged(handler: OnViewChangedFn) {
        this.onViewChangedHandler = handler;
    }

    public onAircraftMoved(handler: OnAircraftMovedFn) {
        this.onAircraftMovedHandler = handler;
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
            this.routeLines[id] = this.createRouteLine(id, this.trackingStatuses[id] ? [this.trackingStatuses[id], status] : [status], status.isOnGround);
        } else {
            if (status.groundSpeed > 0.05) {
                const routeLine = this.routeLines[id];
                routeLine.setCoordinates(routeLine.getCoordinates().concat([new maptalks.Coordinate([status.longitude, status.latitude])]));
                routeLine.setProperties({
                    altitude: routeLine.getProperties().altitude.concat(status.altitude * MaptalksMap.FEET_TO_METER)
                });
            }
        }
        this.trackingStatuses[id] = status;
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
        if (!this.routeLayers[id]) {
            this.routeLayers[id] = new maptalks.VectorLayer('route_' + id, {
                enableAltitude: true,
                drawAltitude: {
                    polygonFill: '#1bbc9b',
                    polygonOpacity: 0.3,
                    lineWidth: 0
                }
            });
            if (this.map) {
                this.routeLayers[id].addTo(this.map);
            }
        }

        let line = new maptalks.LineString(route.map(r => new maptalks.Coordinate([r.longitude, r.latitude])), {
            symbol: {
                lineColor: isOnGround ? ROUTE_GROUND_COLOR : ROUTE_AIR_COLOR,
                lineWidth: 3
            },
            properties: {
                altitude: route.map(r => r.altitude * MaptalksMap.FEET_TO_METER)
            }
        });
        line.addTo(this.routeLayers[id])
        return line;
    }

    public clearTrack(id: string) {
        delete this.trackingStatuses[id];
        if (this.routeLayers[id]) {
            this.routeLayers[id].remove();
            delete this.routeLayers[id];
        }
        delete this.routeLines[id];
    }

    private fitExtent(c1: Coordinate, c2: Coordinate) {
        if (!this.map) return;

        const extent = new maptalks.Extent(c1, c2);
        this.map.setPitch(0);
        this.map.fitExtent(extent, -1);
        this.map.setPitch(30);
    }

    private handleZoom() {
        const radius = this.determineAircraftSize();
        for (let connectionId in this.markers) {
            const marker = this.markers[connectionId];
            marker.aircraft.setRadius(radius);
        }
    }

    private determineAircraftSize() {
        if (!this.map) return MaptalksMap.AIRCRAFT_SIZE;
        return Math.pow(2, 14 - this.map.getZoom()) * MaptalksMap.AIRCRAFT_SIZE;
    }

    private moveAircraft(e: { coordinate: Coordinate }) {
        if (this.onAircraftMovedHandler) {
            this.onAircraftMovedHandler({ latitude: e.coordinate.y, longitude: e.coordinate.x });
        }
        return true;
    }
}