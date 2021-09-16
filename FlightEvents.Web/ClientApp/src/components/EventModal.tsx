import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, ModalFooter } from 'reactstrap';
import { FlightEvent, Airport, FlightPlan, LeaderboardRecord } from '../Models';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import parseJSON from 'date-fns/parseJSON';
import ReactMarkdown from 'react-markdown';
import { HubConnection } from '@microsoft/signalr';
import { convertPropertyNames, pascalCaseToCamelCase } from '../Converters';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from './Leaderboard';
import FlightPlanComponent from './FlightPlanComponent';

interface Props {
    hub: HubConnection;
    flightEvent: FlightEvent;
    isOpen: boolean;
    toggle: () => void;
    onAirportLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
}

interface State {
    leaderboards: Leaderboards;
}

function renderTime(event: FlightEvent) {
    if (event.endDateTime) {
        return <><StyledTime>{parseJSON(event.startDateTime).toLocaleString()}</StyledTime> - <StyledTime>{parseJSON(event.endDateTime).toLocaleString()}</StyledTime></>;
    } else {
        return <StyledTime>{parseJSON(event.startDateTime).toLocaleString()}</StyledTime>;
    }
}

export default class EventModal extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            leaderboards: {}
        }

        this.handleOpen = this.handleOpen.bind(this);
    }

    private async handleOpen() {
        this.props.hub.on("UpdateLeaderboard", (records: LeaderboardRecord[]) => {
            records = convertPropertyNames(records, pascalCaseToCamelCase) as LeaderboardRecord[];
            this.setState({
                leaderboards: recordsToLeaderboards(records)
            })
        })

        await this.props.hub.send("Join", "Leaderboard:" + this.props.flightEvent.id);
    }

    public render() {
        const handleClosed = async () => {
            this.props.hub.off("UpdateLeaderboard");

            await this.props.hub.send("Leave", "Leaderboard:" + this.props.flightEvent.id);

            this.setState({ leaderboards: {} })
        }

        return <Modal isOpen={this.props.isOpen} toggle={this.props.toggle} onOpened={this.handleOpen} onClosed={handleClosed} size='xl'>
            <ModalHeader>{this.props.flightEvent.name}</ModalHeader>
            <ModalBody>
                <Query query={gql`query GetFlightEvent($id: Uuid!) {
    flightEvent(id: $id) {
        id
        type
        name
        description
        startDateTime
        endDateTime
        url
        waypoints
        leaderboards
        checkpoints {
            waypoint
            symbol
        }
    }
}`} variables={{ id: this.props.flightEvent.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
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
                                    React.useEffect(() => {
                                        if (!loading && !error && data) this.props.onAirportLoaded(data.airports);
                                    }, [loading, error, data]);
                                    return null;
                                }}</Query>}

                            <div>{renderTime(event)}</div>
                            <div><StyledReactMarkdown>{event.description}</StyledReactMarkdown></div>
                            {!!event.url && <><h6>Read more at:</h6><a href={event.url} target="_blank" rel="noopener noreferrer">{event.url}</a></>}

                            <FlightPlanComponent id={event.id} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />

                            {event.type === "RACE" && this.state.leaderboards && <Leaderboard event={event} leaderboards={this.state.leaderboards} />}
                        </>;
                    }}</Query>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" disabled>Join</Button>{' '}
                <Button color="secondary" onClick={this.props.toggle}>Close</Button>
            </ModalFooter>
        </Modal>
    }
}

const StyledTime = styled.span`
border-bottom: 1px dashed #909090;
margin-bottom: 10px;
`

const StyledReactMarkdown = styled(ReactMarkdown)`
img {
    max-width: 100%;
}
`