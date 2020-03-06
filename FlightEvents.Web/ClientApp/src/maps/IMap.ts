import { AircraftStatus, Airport, FlightPlan } from '../Models';

export interface IMap {
    initialize(divId: string)
    deinitialize();
    moveMarker(connectionId: string, aircraftStatus: AircraftStatus, isMe: boolean, isFollowing: boolean, isMoreInfo: boolean)
    drawAirports(airports: Airport[])
    drawFlightPlans(flightPlans: FlightPlan[])
    forcusAircraft(aircraftStatus: AircraftStatus)
    cleanUp(connectionId: string, isMe: boolean)
    addRangeCircle()
    removeRangeCircle()
    setTileLayer(type: MapTileType)
}

export enum MapTileType {
    OpenStreetMap,
    OpenTopoMap,
    EsriWorldImagery,
    EsriTopo,
    Carto
}