import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Form, Container, Row, Col, Breadcrumb, BreadcrumbItem, Button, Input, InputGroup, InputGroupAddon, ListGroup } from 'reactstrap';
import { RouteComponentProps, Link } from 'react-router-dom';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { FlightEvent, LeaderboardRecord, Stopwatch } from '../../Models';
import StopwatchHub from '../StopwatchHub';
import StopwatchItem from '../StopwatchItem';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from '../Leaderboard';

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
    stopwatches: { [id: string]: Stopwatch };
    leaderboards: Leaderboards;

    name: string;
    leaderboardName: string;
}

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

window["shift"] = 0;

export default (props: RouteComponentProps<RouteProps>) => {
    const [state, setState] = React.useState<State>({ stopwatches: {}, name: '', leaderboardName: '', leaderboards: {} });

    return <Query query={QUERY} variables={{ id: props.match.params.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
        if (loading) return <>Loading...</>
        if (error) return <>Cannot load event!</>

        const event = data.flightEvent;

        const handleLeaderboardNameChanged = (e) => {
            setState({ ...state, leaderboardName: e.target.value });
        }

        const handleNameChanged = (e) => {
            setState({ ...state, name: e.target.value });
        }

        const handleAddClicked = async (e) => {
            e.preventDefault();
            if (state.leaderboardName && state.name) {
                await hub.send("AddStopwatch", { eventId: event.id, leaderboardName: state.leaderboardName, name: state.name });
                setState({ ...state, name: '' });
            }
        }

        const handleStartAll = async () => {
            await hub.send("StartAllStopwatches", event.id);
        }

        const handleUpdateStopwatch = (stopwatch, serverDateString) => {
            window["shift"] = new Date().getTime() - new Date(serverDateString).getTime();

            setState(state => ({
                ...state, stopwatches: { ...state.stopwatches, [stopwatch.id]: stopwatch }
            }))
        }

        const handleRemoveStopwatch = (stopwatch: Stopwatch) => {
            setState(state => {
                delete state.stopwatches[stopwatch.id];
                return {
                    ...state, stopwatches: { ...state.stopwatches }
                }
            })
        }

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
                onUpdateStopwatch={handleUpdateStopwatch}
                onRemoveStopwatch={handleRemoveStopwatch}
                onUpdateLeaderboard={onUpdateLeaderboard}
            />
            <Row>
                <Col>
                    <Breadcrumb>
                        <BreadcrumbItem><Link to='/'>🗺</Link></BreadcrumbItem>
                        <BreadcrumbItem><Link to='/Events'>Events</Link></BreadcrumbItem>
                        <BreadcrumbItem><Link to={`/Events/${event.id}`}>{event.name}</Link></BreadcrumbItem>
                        <BreadcrumbItem>Stopwatches</BreadcrumbItem>
                    </Breadcrumb>
                </Col>
            </Row>
            <Row>
                <Col md={8}>
                    <Form onSubmit={handleAddClicked}>
                        <InputGroup>
                            <InputGroupAddon addonType="prepend">
                                <select className='form-control' value={state.leaderboardName} onChange={handleLeaderboardNameChanged}>
                                    <option>Select leaderboard...</option>
                                    {event.leaderboards?.map(leaderboardName => <option key={leaderboardName} value={leaderboardName}>{leaderboardName}</option>)}
                                </select>
                            </InputGroupAddon>
                            <Input value={state.name} onChange={handleNameChanged} placeholder="Name" />
                            <InputGroupAddon addonType="append"><Button type="submit">Add</Button></InputGroupAddon>
                        </InputGroup>
                    </Form>
                    <hr />
                    {Object.keys(state.stopwatches).length > 0 && <Button color="primary" onClick={handleStartAll}>Start All</Button>}
                    <br />
                    <ListGroup>
                        {Object.keys(state.stopwatches).map(id => <StopwatchItem key={id} eventId={event.id} hub={hub} {...state.stopwatches[id]} />)}
                    </ListGroup>
                    <br />
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