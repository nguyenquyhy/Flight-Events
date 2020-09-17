import * as React from 'react';
import styled from 'styled-components';
import { Modal, ModalBody, ModalHeader } from 'reactstrap';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { HubConnection } from '@microsoft/signalr';
import { Airport, FlightEvent, FlightPlan } from '../Models';
import EventItem from './EventItem';

interface Props {
    hub: HubConnection;
    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
}

interface State {
    open: boolean;
}

const PastEvents = (props: Props) => {
    const [state, setState] = React.useState<State>({ open: false })

    const handleToggle = () => {
        setState({ open: !state.open })
    }

    return <>
        <button className="btn btn-link" onClick={handleToggle}>Show past events</button>
        <Modal isOpen={state.open} toggle={handleToggle}>
            <ModalHeader>Past Events</ModalHeader>
            <ModalBody>
                <Query query={gql`{
    flightEvents {
        id
        type
        name
        startDateTime
    }
}`}>{({ loading, error, data }: ApolloQueryResult<{ flightEvents: FlightEvent[] }>) => {
                        if (loading) return <>Loading...</>;
                        if (error) return <>Error!</>;

                        const events = data.flightEvents.slice()
                            .sort((a, b) => (new Date(b.startDateTime).getTime() - new Date(a.startDateTime).getTime()));

                        return <>
                            {events.map(event => <List key={event.id}>
                                <EventItem flightEvent={event} hub={props.hub} onAirportsLoaded={props.onAirportsLoaded} onFlightPlansLoaded={props.onFlightPlansLoaded} />
                            </List>)}
                        </>
                    }}
                </Query>
            </ModalBody>
        </Modal>
    </>
}

const List = styled.ul`
list-style: none;
padding: 0;
margin-bottom: 4px;
width: 100%;
`

export default PastEvents;