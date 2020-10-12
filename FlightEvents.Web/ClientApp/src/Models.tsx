export interface ATCInfo {
    callsign: string;

    longitude: number;
    latitude: number;

    realName: string;
    certificate: string;
    rating: string;
}

export interface ATCStatus {
    callsign: string;

    longitude: number;
    latitude: number;

    frequencyCom: number;
}

export interface AircraftStatusBrief {
    longitude: number;
    latitude: number;
    altitude: number;
    isOnGround: boolean;
}

export interface AircraftStatus {
    callsign: string;

    longitude: number;
    latitude: number;
    heading: number;
    trueHeading: number;

    altitude: number;
    altitudeAboveGround: number;
    indicatedAirSpeed: number;

    groundSpeed: number;

    isOnGround: boolean;

    frequencyCom1: number;
}

export interface FlightEvent {
    id: string;
    type: string;
    name: string;
    description: string;
    leaderboards: string[] | null;
    checkpoints: Checkpoint[] | null;
    startDateTime: string;
    endDateTime: string | null;
    url: string | null;
    waypoints: string | null;
    racerCallsigns: string[] | null;
    markedWaypoints: string[] | null;
}

export interface Checkpoint {
    waypoint: string;
    symbol: string;
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

export interface FlightPlanCompact {
    callsign: string;
    departure: string;
    destination: string;
    alternate: string;
    cruisingAltitude: number;
    cruisingSpeed: number;
    route: string;
    remarks: string;
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
    altitude?: number
}

export interface FlightPlanWaypoint {
    id: string
    airway: string
    type: string
    latitude: number
    longitude: number
    altitude?: number
    icao: WaypointICAO
}

export interface WaypointICAO {
    ident
    airport
    region
}

export interface LeaderboardRecord {
    eventId: string;
    playerName: string;
    leaderboardName: string;
    subIndex: number;
    score: number;
    scoreDisplay: string;
    timeSinceStart: number;
}

export interface Stopwatch {
    id: string;
    name: string;
    addedDateTime: string;
    startedDateTime?: string;
    stoppedDateTime?: string;
    lapsDateTime: string[];
    leaderboardName: string;
}