import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Container, Row, Col, Breadcrumb, BreadcrumbItem } from 'reactstrap';
import { RouteComponentProps, Link } from 'react-router-dom';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { FlightEvent, LeaderboardRecord } from '../../Models';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from '../Leaderboard';
import StopwatchHub from '../StopwatchHub';

const QUERY = gql`query getEvent($id: Uuid!) {
    flightEvent(id: $id) {
        id
        name
        description
        startDateTime
        url
        waypoints
        leaderboards
        leaderboardLaps
    }
}`

interface RouteProps {
    id: string;
}

interface State {
    leaderboards: Leaderboards;
}

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

export default (props: RouteComponentProps<RouteProps>) => {
    const [state, setState] = React.useState<State>({ leaderboards: {} });

    return <Query query={QUERY} variables={{ id: props.match.params.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
        if (loading) return <>Loading...</>
        if (error) return <>Cannot load event!</>

        const event = data.flightEvent;

        const onUpdateLeaderboard = (records: LeaderboardRecord[]) => {
            setState(state => ({
                ...state,
                leaderboards: recordsToLeaderboards(records)
            }))
        }

        return <Container>
            <StopwatchHub
                eventId={event.id}
                hub={hub}
                onUpdateStopwatch={null}
                onRemoveStopwatch={null}
                onUpdateLeaderboard={onUpdateLeaderboard}
            />
            <Row>
                <Col>
                    <Breadcrumb>
                        <BreadcrumbItem><Link to='/'>🗺</Link></BreadcrumbItem>
                        <BreadcrumbItem><Link to='/Events'>Events</Link></BreadcrumbItem>
                        <BreadcrumbItem><Link to={`/Events/${event.id}`}>{event.name}</Link></BreadcrumbItem>
                        <BreadcrumbItem>Leaderboard</BreadcrumbItem>
                    </Breadcrumb>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Leaderboard event={event} leaderboards={state.leaderboards} />
                </Col>
            </Row>
        </Container>
    }}
    </Query>
}