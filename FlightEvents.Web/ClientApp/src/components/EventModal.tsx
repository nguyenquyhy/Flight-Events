import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, ModalFooter } from 'reactstrap';
import { FlightEvent, Airport, FlightPlan, LeaderboardRecord } from '../Models';
import Api from '../Api';
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
    isLoading: boolean;
    flightEvent: FlightEvent | null;
    flightPlans: FlightPlan[] | null;
    leaderboards: Leaderboards;
}

export default class EventModal extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            isLoading: true,
            flightEvent: null,
            flightPlans: null,
            leaderboards: {}
        }

        this.handleOpen = this.handleOpen.bind(this);
        this.handleClosed = this.handleClosed.bind(this);
    }

    private airports: Airport[] | null = null;

    componentDidMount() {
    }

    componentWillUnmount() {
    }

    private async handleOpen() {
        this.props.hub.on("UpdateLeaderboard", (records: LeaderboardRecord[]) => {
            records = convertPropertyNames(records, pascalCaseToCamelCase) as LeaderboardRecord[];
            this.setState({
                leaderboards: recordsToLeaderboards(records)
            })
        })

        if (!this.state.flightEvent) {
            const event = await Api.getFlightEvent(this.props.flightEvent.id);
            this.setState({
                isLoading: false,
                flightEvent: event
            });
            if (event.waypoints) {
                if (!this.airports) {
                    this.airports = await Api.getAirports(event.waypoints.split(' '));
                }
            }

            if (!this.state.flightPlans) {
                const flightPlans = await Api.getFlightPlans(event.id);
                this.setState({ flightPlans: flightPlans }, () => {
                    this.props.onFlightPlansLoaded(flightPlans);
                })
            }

            await this.props.hub.send("Join", "Leaderboard:" + event.id);
        }

        if (this.airports) {
            this.props.onAirportLoaded(this.airports);
        }

        if (this.state.flightPlans) {
            this.props.onFlightPlansLoaded(this.state.flightPlans);
        }
    }

    private async handleClosed() {
        this.props.hub.off("UpdateLeaderboard");

        if (this.state.flightEvent) {
            await this.props.hub.send("Leave", "Leaderboard:" + this.state.flightEvent.id);
        }

        this.setState({ leaderboards: {} })
    }

    public render() {
        const details = this.state.flightEvent ?
            <>
                <div><StyledTime>{parseJSON(this.state.flightEvent.startDateTime).toLocaleString()}</StyledTime></div>
                <div><ReactMarkdown>{this.state.flightEvent.description}</ReactMarkdown></div>
                {!!this.state.flightEvent.url && <><h6>Read more at:</h6><a href={this.state.flightEvent.url} target="_blank" rel="noopener noreferrer">{this.state.flightEvent.url}</a></>}

                {this.state.flightPlans && <>
                    <Header>Flight Plans</Header>
                    {this.state.flightPlans.length === 0 ?
                        <p><em>No flight plan is available for this event.</em></p> :
                        <ul>
                            {this.state.flightPlans.map(flightPlan => (
                                <li key={flightPlan.id}><a href={flightPlan.downloadUrl} target="_blank" rel="noopener noreferrer" download={flightPlan.id}>{flightPlan.data.title}</a></li>
                            ))}
                        </ul>
                    }
                </>}

                {this.state.flightEvent.type === "RACE" && this.state.leaderboards && <Leaderboard event={this.state.flightEvent} leaderboards={this.state.leaderboards} />}
            </> :
            <div>Loading...</div>;


        return <Modal isOpen={this.props.isOpen} toggle={this.props.toggle} onOpened={this.handleOpen} onClosed={this.handleClosed} size='lg'>
            <ModalHeader>{this.props.flightEvent.name}</ModalHeader>
            <ModalBody>
                {details}
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