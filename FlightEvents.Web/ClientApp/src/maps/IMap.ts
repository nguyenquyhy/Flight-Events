import { ATCStatus, ATCInfo, AircraftStatus, Airport, FlightPlanData, AircraftStatusBrief } from '../Models';

export interface IMap {
    initialize(divId: string, view?: View)
    deinitialize();

    moveATCMarker(connectionId: string, atcStatus: ATCStatus | null, atcInfo: ATCInfo | null)
    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isShowingPlan: boolean, isMoreInfo: boolean, isShowingRoute: boolean)
    drawAirports(airports: Airport[])
    drawFlightPlans(flightPlans: FlightPlanData[])
    focusAircraft(aircraftStatus: AircraftStatus)
    cleanUp(connectionId: string, isMe: boolean)
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
}

export type OnViewChangedFn = (view: View) => void;

export interface View {
    latitude: number
    longitude: number
    zoom: number
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