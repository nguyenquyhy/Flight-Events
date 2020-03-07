import { IMap, MapTileType } from './IMap';
import * as maptalks from 'maptalks';
import { AircraftStatus, Airport, FlightPlan } from '../Models';
import { MAPBOX_API_KEY } from '../Constants';

interface Markers {
    aircraft: Sector
    aircraftLine: Marker
    info: Label
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
    addGeometry: (markers: Geometry | Geometry[]) => void;
}

interface Geometry {
    show: () => void;
    hide: () => void;
    remove: () => void;
    setProperties: (props: any) => void;
    updateSymbol: (props: any) => void;
    animate: (animation: any, props: any) => void;
}

interface Marker extends Geometry {
    setCoordinates: (coordinate: Coordinate) => void;
    getCoordinates: () => Coordinate;
}

interface Label extends Marker {
    setContent: (content: string) => void;
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

export default class MaptalksMap implements IMap {
    static AIRCRAFT_SIZE = 120;
    static ANIMATION_DURATION = 500;
    static FEET_TO_METER = .3048;

    map: Map | undefined;
    markers: { [connectionId: string]: Markers } = {};

    visibleCircle: Circle = new maptalks.Circle([0, 0], 3048, {
        symbol: {
            lineColor: '#000000',
            lineWidth: 2,
            lineOpacity: 0.25,
            polygonFill: 'none',
            polygonOpacity: 0,
        }
    })

    aircraftLayer: VectorLayer | undefined;

    initialize(divId: string) {
        const map = new maptalks.Map(divId, {
            center: [-0.113049, 51.498568],
            zoom: 14
        });

        map.on('zoomend', () => this.handleZoom());

        const aircraftLayer = new maptalks.VectorLayer('vector', {
            enableAltitude: true,
            // draw altitude
            drawAltitude: {
                lineWidth: 1,
                lineColor: '#aaa'
            }
        }).addTo(map);

        aircraftLayer.addGeometry(this.visibleCircle);

        this.visibleCircle.hide();

        this.aircraftLayer = aircraftLayer;
        this.map = map;
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

            markers.info.animate({ translate: [offset['x'], offset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });

            if (isMe) {
                const circleOffset = latlng.sub(this.visibleCircle.getCoordinates());
                this.visibleCircle.setProperties({ altitude: altitude });
                this.visibleCircle.animate({ translate: [circleOffset['x'], circleOffset['y']] }, { duration: MaptalksMap.ANIMATION_DURATION });
            }

            if (isMe) {
                markers.aircraft.updateSymbol({
                    polygonFill: 'rgb(135,196,240)'
                })
            } else {
                markers.aircraft.updateSymbol({
                    polygonFill: '#34495e'
                })
            }

            if (isMoreInfo) {
                const body = `${aircraftStatus.Callsign}\nALT ${Math.round(aircraftStatus.Altitude)} ft\nHDG ${Math.round(aircraftStatus.Heading)}\u00B0\nIAS ${Math.round(aircraftStatus.IndicatedAirSpeed)} kts`
                markers.info.setContent(body);
            } else {
                const body = `${aircraftStatus.Callsign}`
                markers.info.setContent(body);
            }
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
            })
            const aircraft = new maptalks.Sector(latlng, MaptalksMap.AIRCRAFT_SIZE, -aircraftStatus.TrueHeading - 10 - 90, -aircraftStatus.TrueHeading + 10 - 90, {
                symbol: {
                    lineColor: '#34495e',
                    lineWidth: 2,
                    polygonFill: '#34495e',
                    polygonOpacity: 1
                },
                properties: {
                    altitude: aircraftStatus.Altitude * MaptalksMap.FEET_TO_METER
                }
            })
            const info = new maptalks.Label(`${aircraftStatus.Callsign}`, latlng, {
                textSymbol: {
                    textFaceName: 'Lucida Console',
                    textFill: '#34495e',
                    textSize: 12,
                    textWeight: 'bold',
                    textVerticalAlignment: 'top',
                    textHorizontalAlignment: 'right'
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
                    altitude: aircraftStatus.Altitude * MaptalksMap.FEET_TO_METER
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

        let popup = `Altitude: ${Math.floor(aircraftStatus.Altitude)}<br />Airspeed: ${Math.floor(aircraftStatus.IndicatedAirSpeed)}`;
        if (aircraftStatus.Callsign) {
            popup = `<b>${aircraftStatus.Callsign}</b><br />${popup}`;
        }
    }

    drawAirports(airports: Airport[]) {
        // TODO:
    }

    drawFlightPlans(flightPlans: FlightPlan[]) {
        // TODO:
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
            marker.aircraftLine.remove();
            marker.info.remove();
            if (isMe) {
                this.visibleCircle.hide();
            }
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