import * as React from 'react';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult, gql } from '@apollo/client';
import { Airport, FlightEvent, FlightPlan } from '../Models';
import FlightPlanComponent from './FlightPlanComponent';

interface FlightPlanLoaderProps {
    eventId: string;

    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
}

const FlightPlanLoader = (props: FlightPlanLoaderProps) => {
    return <>
        <Query query={gql`query GetFlightEvent($id: Uuid!) {
    flightEvent(id: $id) {
        id
        waypoints
    }
}`} variables={{ id: props.eventId }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
                if (loading) return <>Loading...</>
                if (error) return <>Error!</>

                const event = data.flightEvent;

                return <>
                    {!!event.waypoints && <Query query={gql`query GetAirports($idents: [String]!) {
    airports(idents: $idents) {
        ident
        name
        longitude
        latitude
    }
}`} variables={{ idents: event.waypoints.split(' ') }}>{({ loading, error, data }: ApolloQueryResult<{ airports: Airport[] }>) => {
                            if (!loading && !error && data) props.onAirportsLoaded(data.airports);
                            return null;
                        }}</Query>}
                    <FlightPlanComponent id={event.id} onFlightPlansLoaded={props.onFlightPlansLoaded} hideList={true} />
                </>;
            }}</Query>
    </>
}

export default FlightPlanLoader;