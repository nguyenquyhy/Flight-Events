﻿import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Query, Mutation } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { RouteComponentProps, Link } from 'react-router-dom';
import { Container, Row, Col, Button, FormGroup, Label, Input, Breadcrumb, BreadcrumbItem } from 'reactstrap';
import { FlightEvent } from '../../Models';

const GET_QUERY = gql`
query getEvent($id: UUID!) {
    flightEvent(id: $id) {
        id
        type
        name
        description
        url
        startDateTime
        endDateTime
    }
}
`

const UPDATE_QUERY = gql`
mutation Update($flightEvent: FlightEventUpdate!) {
    updateFlightEvent(flightEvent: $flightEvent) {
        id
        type
        name
        description
        url
        startDateTime
        endDateTime
    }
}`

interface State {
    name: string;
    description: string;
    url: string | null;
    startDateTime: string;
    endDateTime: string | null;
}

interface RouteProps {
    id: string;
}

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

export default (props: RouteComponentProps<RouteProps>) => {
    React.useEffect(() => {
        (async () => {
            hub.onreconnected(async connectionId => {
                console.log('Connected to SignalR with connection ID ' + connectionId);
            });

            await hub.start();
        })();
    }, []);

    return (
        <Query query={GET_QUERY} variables={{ id: props.match.params.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
            if (loading) return <>Loading...</>
            if (error) return <>Error!</>

            return <Container>
                <Row>
                    <Col>
                        <Breadcrumb>
                            <BreadcrumbItem><Link to='/'>🗺</Link></BreadcrumbItem>
                            <BreadcrumbItem><Link to='/Events'>Events</Link></BreadcrumbItem>
                            <BreadcrumbItem>{data.flightEvent.name}</BreadcrumbItem>
                        </Breadcrumb>
                    </Col>
                </Row>
                <Row>
                    <Col>
                        <Mutated event={data.flightEvent} />
                    </Col>
                </Row>
            </Container>
        }}</Query>
    )
}

interface MutatedProps {
    event: FlightEvent;
}

const Mutated = (props: MutatedProps) => {
    const [state, setState] = React.useState<State>({
        name: props.event.name,
        description: props.event.description,
        url: props.event.url,
        startDateTime: props.event.startDateTime,
        endDateTime: props.event.endDateTime
    })

    const handleNameChanged = (e) => setState({ ...state, name: e.target.value });
    const handleDescriptionChanged = (e) => setState({ ...state, description: e.target.value });
    const handleUrlChanged = (e) => setState({ ...state, url: e.target.value ?? null });
    const handleStartDateTimeChanged = (e) => setState({ ...state, startDateTime: e.target.value });
    const handleEndDateTimeChanged = (e) => setState({ ...state, endDateTime: e.target.value ?? null });

    return <Mutation mutation={UPDATE_QUERY} key={props.event.id}>
        {update => (
            <form onSubmit={e => {
                e.preventDefault();
                update({ variables: { flightEvent: { id: props.event.id, name: state.name, description: state.description, url: state.url, startDateTime: state.startDateTime } } })
            }}>
                <h2>Event</h2>

                <FormGroup>
                    <Label>Name</Label>
                    <Input value={state.name} onChange={handleNameChanged} />
                </FormGroup>
                <FormGroup>
                    <Label>Description</Label>
                    <Input type="textarea" value={state.description} onChange={handleDescriptionChanged} />
                </FormGroup>
                <FormGroup>
                    <Label>URL</Label>
                    <Input type="url" value={state.url ?? ""} onChange={handleUrlChanged} />
                </FormGroup>
                <FormGroup>
                    <Label>Start</Label>
                    <Input value={state.startDateTime} onChange={handleStartDateTimeChanged} />
                </FormGroup>
                <FormGroup>
                    <Label>End</Label>
                    <Input value={state.endDateTime ?? ""} onChange={handleEndDateTimeChanged} />
                </FormGroup>

                <Button color="primary" type="submit">Update Event</Button>
            </form>
        )}
    </Mutation>
}