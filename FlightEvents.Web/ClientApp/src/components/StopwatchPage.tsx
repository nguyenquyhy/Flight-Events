import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Form, Container, Row, Col, Breadcrumb, BreadcrumbItem, Button, Input, InputGroup, InputGroupAddon, ListGroup } from 'reactstrap';
import { RouteComponentProps } from 'react-router-dom';
import Api from '../Api';
import { FlightEvent, LeaderboardRecord, Stopwatch } from '../Models';
import StopwatchItem from './StopwatchItem';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from './Leaderboard';

interface RouteProps {
    eventCode: string;
}

interface State {
    event: FlightEvent | null;
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

    const [state, setState] = React.useState<State>({ event: null, stopwatches: {}, name: '', leaderboardName: '', leaderboards: {} });

    React.useEffect(() => {
        const f = async () => {
            const event = await Api.getFlightEventByStopwatchCode(eventCode);
            setState(state => ({ ...state, event: event }));
        }
        f();
    }, [eventCode])

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
                    <BreadcrumbItem>{state.event?.name}</BreadcrumbItem>
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
                                {state.event?.leaderboards?.map(leaderboardName => <option key={leaderboardName} value={leaderboardName}>{leaderboardName}</option>)}
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
                {state.event && <Leaderboard event={state.event} leaderboards={state.leaderboards} />}
            </Col>
        </Row>
    </Container>
}

export default StopwatchPage;