import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Form, Container, Row, Col, Breadcrumb, BreadcrumbItem, Button, Input, InputGroup, InputGroupAddon, ListGroup } from 'reactstrap';
import { RouteComponentProps } from 'react-router-dom';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { FlightEvent, LeaderboardRecord, Stopwatch } from '../Models';
import StopwatchItem from './StopwatchItem';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from './Leaderboard';

interface RouteProps {
    eventCode: string;
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

const StopwatchPage = (props: RouteComponentProps<RouteProps>) => {
    const eventCode = props.match.params.eventCode;

    const [state, setState] = React.useState<State>({ stopwatches: {}, name: '', leaderboardName: '', leaderboards: {} });

    React.useEffect(() => {
        const f = async () => {
            hub.on("UpdateStopwatch", (stopwatch: Stopwatch, serverDateString: string) => {
                window["shift"] = new Date().getTime() - new Date(serverDateString).getTime();

                setState(state => ({
                    ...state, stopwatches: { ...state.stopwatches, [stopwatch.id]: stopwatch }
                }))
            });
            hub.on("RemoveStopwatch", (stopwatch: Stopwatch) => {
                setState(state => {
                    delete state.stopwatches[stopwatch.id];
                    return {
                        ...state, stopwatches: { ...state.stopwatches }
                    }
                })
            });
            hub.on("UpdateLeaderboard", (records: LeaderboardRecord[]) => {
                setState(state => ({
                    ...state,
                    leaderboards: recordsToLeaderboards(records)
                }))
            })
            await hub.start();
            await hub.send("Join", "Stopwatch:" + eventCode);
        }
        f();

        return () => {
            hub.stop();
        }
    }, [eventCode])

    return <Query query={gql`query ($code: String!) {
    flightEventByStopwatchCode(code: $code) {
        id
        name
        description
        startDateTime
        url
        waypoints
        leaderboards
        leaderboardLaps
    }
}`} variables={{ code: props.match.params.eventCode }}>{({ loading, error, data }: ApolloQueryResult<{ flightEventByStopwatchCode: FlightEvent }>) => {
            if (loading) return <>Loading...</>
            if (error) return <>Error!</>

            const event = data.flightEventByStopwatchCode;

            const handleLeaderboardNameChanged = (e) => {
                setState({ ...state, leaderboardName: e.target.value });
            }

            const handleNameChanged = (e) => {
                setState({ ...state, name: e.target.value });
            }

            const handleAddClicked = async (e) => {
                e.preventDefault();
                if (state.leaderboardName && state.name) {
                    await hub.send("AddStopwatch", { eventCode: eventCode, leaderboardName: state.leaderboardName, name: state.name });
                    setState({ ...state, name: '' });
                }
            }

            const handleStartAll = async () => {
                await hub.send("StartAllStopwatches", eventCode);
            }

            return <Container>
                <Row>
                    <Col>
                        <Breadcrumb>
                            <BreadcrumbItem>Events</BreadcrumbItem>
                            <BreadcrumbItem>{event.name}</BreadcrumbItem>
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
                            {Object.keys(state.stopwatches).map(id => <StopwatchItem key={id} eventCode={eventCode} hub={hub} {...state.stopwatches[id]} />)}
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

export default StopwatchPage;