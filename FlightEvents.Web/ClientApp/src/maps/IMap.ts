﻿import { ATCStatus, ATCInfo, AircraftStatus, Airport, FlightPlanData, AircraftStatusBrief } from '../Models';

export interface IMap {
    initialize(divId: string, view: View | undefined, mode: string | null)
    deinitialize();

    moveATCMarker(clientId: string, atcStatus: ATCStatus | null, atcInfo: ATCInfo | null)
    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isShowingPlan: boolean, isMoreInfo: boolean, isShowingRoute: boolean)
    drawAirports(airports: Airport[])
    drawFlightPlans(flightPlans: FlightPlanData[])
    focus(location: { longitude: number, latitude: number }, zoom?: number)
    cleanUpController(clientId: string)
    cleanUpAircraft(clientId: string, isMe: boolean)
    addRangeCircle()
    removeRangeCircle()
    setTileLayer(type: MapTileType)

    onViewChanged(handler: OnViewChangedFn);
    onAircraftMoved(handler: OnAircraftMovedFn);
    onSetMe(handler: OnSetOptionalClientIdFn);
    onSetFollow(handler: OnSetOptionalClientIdFn);
    onSetShowPlan(handler: OnSetOptionalClientIdFn);
    onSetShowInfo(handler: OnSetClientIdFn);
    onSetShowRoute(handler: OnSetClientIdFn);

    track(id: string, status: AircraftStatus);
    prependTrack(id: string, route: AircraftStatusBrief[]);
    clearTrack(id: string);

    changeMode(dark: boolean);

    startDrawing();
}

export type OnViewChangedFn = (view: View) => void;

export interface View {
    latitude: number | null
    longitude: number | null
    zoom: number | null
}

export type OnAircraftMovedFn = (position: MapPosition) => void;

export interface MapPosition {
    latitude: number, longitude: number
}

export type OnSetOptionalClientIdFn = (clientId: string | null) => void;
export type OnSetClientIdFn = (clientId: string) => void;

export enum MapTileType {
    OpenStreetMap,
    OpenTopoMap,
    EsriWorldImagery,
    EsriTopo,
    Carto,
    UsVfrSectional,
}