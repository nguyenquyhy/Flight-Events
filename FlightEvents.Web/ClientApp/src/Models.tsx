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
}