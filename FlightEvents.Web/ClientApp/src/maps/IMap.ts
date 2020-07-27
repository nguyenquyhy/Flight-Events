import { ATCStatus, ATCInfo, AircraftStatus, Airport, FlightPlanData, AircraftStatusBrief } from '../Models';

export interface IMap {
    initialize(divId: string, view?: View)
    deinitialize();

    moveATCMarker(connectionId: string, atcStatus: ATCStatus | null, atcInfo: ATCInfo | null)
    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isMoreInfo: boolean)
    drawAirports(airports: Airport[])
    drawFlightPlans(flightPlans: FlightPlanData[])
    focusAircraft(aircraftStatus: AircraftStatus)
    cleanUp(connectionId: string, isMe: boolean)
    addRangeCircle()
    removeRangeCircle()
    setTileLayer(type: MapTileType)

    onViewChanged(handler: OnViewChangedFn);
    onAircraftMoved(handler: OnAircraftMovedFn);

    track(id: string, status: AircraftStatus);
    prependTrack(id: string, route: AircraftStatusBrief[]);
    clearTrack(id: string);

    changeMode(dark: boolean);
}

export type OnViewChangedFn = (view: View) => void;

export type OnAircraftMovedFn = (position: { latitude: number, longitude: number, altitude: number }) => void;

export interface View {
    latitude: number
    longitude: number
    zoom: number
}

export enum MapTileType {
    OpenStreetMap,
    OpenTopoMap,
    EsriWorldImagery,
    EsriTopo,
    Carto,
    UsVfrSectional,
}