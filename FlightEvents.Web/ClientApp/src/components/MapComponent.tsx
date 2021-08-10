import * as React from 'react';
import * as _ from 'lodash';
import { HubConnection } from '@microsoft/signalr';
import { IMap, MapTileType, View, MapDimension, MapMode, MapPosition, OnSetOptionalClientIdFn } from '../maps/IMap';
import LeafletMap from '../maps/LeaftletMap';
import MaptalksMap from '../maps/MaptalksMap';
import { AircraftStatus, AircraftStatusBrief, Airport, ATCInfo, ATCStatus, FlightPlanData } from '../Models';
import { convertPropertyNames, pascalCaseToCamelCase } from '../Converters';

interface Props {
    hub: HubConnection;

    mode: MapMode | null;
    isDark: boolean;
    dimension: MapDimension;
    tileType: MapTileType;
    initialView: View;

    allClientIds: string[];

    myClientId: string | null;
    onMeChanged: OnSetOptionalClientIdFn;

    followingClientId: string | null;
    onFollowingChanged: OnSetOptionalClientIdFn;

    flightPlanClientId: string | null;
    onShowPlanChanged: OnSetOptionalClientIdFn;

    showInfoClientIds: string[];
    onShowInfoChanged: (clientId: string) => void;

    showRouteClientIds: string[];
    onShowRouteChanged: (clientId: string) => void;
    onRequestFlightRoute: (clientId: string) => void;
    prependingRoute: { clientId: string, route: AircraftStatusBrief[] } | null;

    onTeleportPositionSelected: (position: MapPosition) => void;

    flightPlans: FlightPlanData[];
    airports: Airport[];
    controllers: { [clientId: string]: ATCInfo & ATCStatus }

    focusView: View | null;
    callsignFilter: string[] | null;

    isDrawing: boolean;
    onDrawingCompleted: () => void;
}

var map: IMap;

const MapComponent = (props: Props) => {
    const firstFocus = React.useRef(false);
    const previousAircraftClientIds = React.useRef<string[]>([]);
    const previousControllerClientIds = React.useRef<string[]>([]);
    const currentView = React.useRef<View>();

    if (map) {
        // HACK to make sure the current view is retained when switching map
        currentView.current = map.getCurrentView();
    }

    React.useEffect(() => {
        console.info('Create map');
        map = props.dimension === MapDimension._2D ? new LeafletMap() : new MaptalksMap();
    }, [props.dimension]);

    React.useEffect(() => {
        console.info('Initialize map');
        if (!currentView.current) {
            currentView.current = props.initialView;
        }
        map.initialize('mapid', currentView.current, props.mode);

        return () => {
            map?.deinitialize();
        };
    }, [props.dimension, props.initialView, props.mode]);

    React.useEffect(() => {
        map.setTileLayer(props.tileType);
    }, [props.dimension, props.tileType]);

    React.useEffect(() => {
        map.onSetMe(props.onMeChanged);
        map.onSetFollow(props.onFollowingChanged);
        map.onSetShowPlan(props.onShowPlanChanged);
        map.onSetShowInfo(props.onShowInfoChanged);
        map.onSetShowRoute(props.onShowRouteChanged);
        map.onTeleportPositionSelected(props.onTeleportPositionSelected);
    }, [props.onMeChanged, props.onFollowingChanged, props.onShowPlanChanged, props.onShowInfoChanged, props.onShowRouteChanged, props.onTeleportPositionSelected]);

    React.useEffect(() => {
        map.changeMode(props.isDark);
    }, [props.dimension, props.isDark]);

    React.useEffect(() => {
        map.setTileLayer(props.tileType);
    }, [props.dimension, props.tileType]);

    React.useEffect(() => {
        if (props.myClientId) {
            map.addRangeCircle();
        } else {
            map.removeRangeCircle();
        }
    }, [props.dimension, props.myClientId]);

    React.useEffect(() => {
        if (props.flightPlanClientId == null) {
            // Clear map
            map.drawFlightPlans([]);
        }
    }, [props.dimension, props.flightPlanClientId]);

    React.useEffect(() => {
        map.drawFlightPlans(props.flightPlans);
    }, [props.dimension, props.flightPlans]);

    React.useEffect(() => {
        if (props.airports.length > 0) {
            map.drawAirports(props.airports);
        }
    }, [props.dimension, props.airports]);

    const hasInitialView = !!props.initialView.latitude && !!props.initialView.longitude;

    React.useEffect(() => {
        const updateHandler = (clientId, aircraftStatus: AircraftStatus) => {
            aircraftStatus = convertPropertyNames(aircraftStatus, pascalCaseToCamelCase);

            if (props.callsignFilter && !props.callsignFilter.includes(aircraftStatus.callsign)) {
                return
            }

            try {
                const isReady = !(Math.abs(aircraftStatus.latitude) < 0.02 && Math.abs(aircraftStatus.longitude) < 0.02);

                if (isReady) {
                    map.moveMarker(clientId, aircraftStatus,
                        props.myClientId === clientId,
                        props.followingClientId === clientId,
                        props.flightPlanClientId === clientId,
                        props.showInfoClientIds.includes(clientId),
                        props.showRouteClientIds.includes(clientId)
                    );
                } else {
                    // Aircraft not loaded
                    map.cleanUpAircraft(clientId, props.myClientId === clientId);
                }

                if (isReady && props.showRouteClientIds.includes(clientId)) {
                    map.track(clientId, aircraftStatus);
                } else {
                    map.clearTrack(clientId);
                }

                //const focusAircraft = !props.followingClientId && !props.initialView.latitude && !props.initialView.longitude && !props.eventId

                // Focus the first ready aircraft
                if (!firstFocus.current && isReady && !hasInitialView) {
                    firstFocus.current = true;
                    map.focus(aircraftStatus);
                }
                // Follow an aircraft
                else if (isReady && props.followingClientId === clientId) {
                    map.focus(aircraftStatus);
                }
            } catch (e) {
                console.error(e);
            }
        }

        props.hub.on("UpdateAircraft", updateHandler);

        return () => {
            props.hub.off("UpdateAircraft", updateHandler);
        }
    }, [props.hub, props.myClientId, props.followingClientId, props.flightPlanClientId, props.showInfoClientIds, props.showRouteClientIds, props.callsignFilter, hasInitialView]);

    React.useEffect(() => {
        const toRemove = _.difference(previousAircraftClientIds.current, props.allClientIds);
        previousAircraftClientIds.current = props.allClientIds;

        for (let clientId of toRemove) {
            map.cleanUpAircraft(clientId, clientId === props.myClientId);
        }
    }, [props.allClientIds, props.myClientId])

    React.useEffect(() => {
        const currentClientIds = Object.keys(props.controllers);
        const toRemove = _.difference(previousControllerClientIds.current, currentClientIds);
        previousControllerClientIds.current = currentClientIds;

        for (let clientId of toRemove) {
            map.cleanUpController(clientId);
        }

        for (let [clientId, atc] of Object.entries(props.controllers)) {
            map.moveATCMarker(clientId, atc, atc);
        }
    }, [props.controllers]);

    const { showRouteClientIds, onRequestFlightRoute, isDrawing, onDrawingCompleted } = props;

    React.useEffect(() => {
        for (let clientId of showRouteClientIds) {
            onRequestFlightRoute(clientId);
        }
    }, [props.dimension, showRouteClientIds, onRequestFlightRoute]);

    React.useEffect(() => {
        if (props.prependingRoute) {
            map.prependTrack(props.prependingRoute.clientId, props.prependingRoute.route);
        }
    }, [props.dimension, props.prependingRoute])

    React.useEffect(() => {
        if (props.focusView && props.focusView.latitude && props.focusView.longitude) {
            map.focus({ longitude: props.focusView.longitude, latitude: props.focusView.latitude }, props.focusView.zoom || undefined);
        }
    }, [props.focusView]);

    React.useEffect(() => {
        if (isDrawing) {
            map.startDrawing();
            onDrawingCompleted();
        }
    }, [isDrawing, onDrawingCompleted]);

    return <>
        {props.isDark && <style dangerouslySetInnerHTML={{ __html: `.leaflet-container { background-color: black } .leaflet-tile, .icon-aircraft-marker { -webkit-filter: hue-rotate(180deg) invert(100%); }` }} />}
        <div id="mapid" style={{ height: '100%' }}></div>
    </>
};

export default MapComponent;