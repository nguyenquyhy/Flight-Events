import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, ModalFooter } from 'reactstrap';
import { ApolloResult, FlightEvent, Airport, FlightPlan, LeaderboardRecord } from '../Models';
import { Query } from 'react-apollo';
import gql from 'graphql-tag';
import parseJSON from 'date-fns/parseJSON';
import ReactMarkdown from 'react-markdown';
import { HubConnection } from '@microsoft/signalr';
import { convertPropertyNames, pascalCaseToCamelCase } from '../Converters';
import Leaderboard, { Leaderboards, recordsToLeaderboards } from './Leaderboard';

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
        url
        waypoints
        leaderboards
        leaderboardLaps
    }
}`} variables={{ id: this.props.flightEvent.id }}>{({ loading, error, data }: ApolloResult<{ flightEvent: FlightEvent }>) => {
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
}`} variables={{ idents: event.waypoints.split(' ') }}>{({ loading, error, data }: ApolloResult<{ airports: Airport[] }>) => {
                                    if (!loading && !error && data) this.props.onAirportLoaded(data.airports);
                                    return null;
                                }}</Query>}

                            <div><StyledTime>{parseJSON(event.startDateTime).toLocaleString()}</StyledTime></div>
                            <div><ReactMarkdown>{event.description}</ReactMarkdown></div>
                            {!!event.url && <><h6>Read more at:</h6><a href={event.url} target="_blank" rel="noopener noreferrer">{event.url}</a></>}

                            <Query query={gql`query GetFlightPlans($id: Uuid!) {
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
}`} variables={{ id: event.id }}>{({ loading, error, data }: ApolloResult<{ flightEvent: { flightPlans: FlightPlan[] } }>) => {
                                    if (loading) return <div>Checking flight plan...</div>;
                                    if (error) return <div>Error!</div>;

                                    const flightPlans = data.flightEvent.flightPlans;

                                    this.props.onFlightPlansLoaded(flightPlans);

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
                                }}
                            </Query>

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

const Header = styled.h5`
margin-top: 12px;
`;