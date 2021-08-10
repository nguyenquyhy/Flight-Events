import * as React from 'react';
import styled from 'styled-components';
import { FlightPlan } from '../Models';
import { gql, useQuery } from '@apollo/client';

const Header = styled.h5`
margin-top: 12px;
`;

const FlightPlanComponent = (props: {
    id: string,
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
    hideList?: boolean;
}) => {
    const { loading, error, data } = useQuery<{ flightEvent: { flightPlans: FlightPlan[] } }>(gql`query GetFlightPlans($id: Uuid!) {
    flightEvent(id: $id) {
        id
        flightPlans {
            id
            downloadUrl
            data {
                title
                description
                cruisingAltitude
                type
                routeType
                departure {
                    id
                    name
                    latitude
                    longitude
                }
                destination {
                    id
                    name
                    latitude
                    longitude
                }
                waypoints {
                    id
                    airway
                    type
                    latitude
                    longitude
                    icao {
                        ident
                        airport
                        region
                    }
                }
            }
        }
    }
}`, { variables: { id: props.id } });

    const onFlightPlansLoaded = props.onFlightPlansLoaded;
    React.useEffect(() => {
        if (!loading && !error && data) {
            onFlightPlansLoaded(data.flightEvent.flightPlans);
        }
    }, [loading, error, data, onFlightPlansLoaded]);

    if (loading) return <div>Checking flight plan...</div>;
    if (error || !data) return <div>Cannot load flight plan!</div>;

    const flightPlans = data.flightEvent.flightPlans;

    if (props.hideList) {
        return null;
    }

    return <>
        <Header>Flight Plans</Header>
        {flightPlans.length === 0 ?
            <p><em>No flight plan is available for this event.</em></p> :
            <ul>
                {flightPlans.map(flightPlan => (
                    <li key={flightPlan.id}><a href={flightPlan.downloadUrl} target="_blank" rel="noopener noreferrer" download={flightPlan.id}>{flightPlan.data.title}</a></li>
                ))}
            </ul>
        }
    </>;
}

export default FlightPlanComponent;