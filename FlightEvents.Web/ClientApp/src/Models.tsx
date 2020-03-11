export interface AircraftStatus {
    callsign: string;

    longitude: number;
    latitude: number;
    heading: number;
    trueHeading: number;

    altitude: number;
    altitudeAboveGround: number;
    indicatedAirSpeed: number;
}

export interface FlightEvent {
    id: string;
    name: string;
    description: string;
    startDateTime: string;
    url: string | null;
    waypoints: string | null;
}

export interface Airport {
    ident: string;
    name: string;
    longitude: number;
    latitude: number;
}

export interface FlightPlan {
    id: string
    data: FlightPlanData
    downloadUrl: string;
}

export interface FlightPlanData {
    title: string
    description: string
    cruisingAltitude: number
    type: "IFR" | "VFR"
    routeType: string
    departure: FlightPlanPosition
    destination: FlightPlanPosition
    waypoints: FlightPlanWaypoint[]
}

export interface FlightPlanPosition {
    id: string
    name: string
    latitude: number
    longitude: number
}

export interface FlightPlanWaypoint {
    id: string
    airway: string
    type: string
    latitude: number
    longitude: number
    icao: WaypointICAO
}

export interface WaypointICAO {
    ident
    airport
    region
}