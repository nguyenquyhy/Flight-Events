import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, ModalFooter } from 'reactstrap';
import { FlightEvent, Airport, FlightPlan } from '../Models';
import Api from '../Api';
import parseISO from 'date-fns/parseISO';
import ReactMarkdown from 'react-markdown';

interface Props {
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
}

export default class EventModal extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            isLoading: true,
            flightEvent: null,
            flightPlans: null
        }

        this.handleOpen = this.handleOpen.bind(this);
    }

    private airports: Airport[] | null = null;

    private async handleOpen() {
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
                this.props.onAirportLoaded(this.airports);
            }

            if (!this.state.flightPlans) {
                const flightPlans = await Api.getFlightPlans(event.id);
                this.setState({ flightPlans: flightPlans }, () => {
                    this.props.onFlightPlansLoaded(flightPlans);
                })
            } else {
                this.props.onFlightPlansLoaded(this.state.flightPlans);
            }
        }

    }

    public render() {
        const details = this.state.flightEvent ?
            <>
                <div><StyledTime>{parseISO(this.state.flightEvent.startDateTime).toLocaleString()}</StyledTime></div>
                <div><ReactMarkdown>{this.state.flightEvent.description}</ReactMarkdown></div>
                {!!this.state.flightEvent.url && <div>URL: <a href={this.state.flightEvent.url} target="_blank">{this.state.flightEvent.url}</a></div>}

                {this.state.flightPlans && <>
                    <h4>Flight Plans</h4>

                    <ul>
                        {this.state.flightPlans.map(flightPlan => (
                            <li><a href={flightPlan.downloadUrl} target="_blank" download={flightPlan.id}>{flightPlan.data.title}</a></li>
                        ))}
                    </ul>
                </>}
            </> :
            <div>Loading...</div>;


        return <Modal isOpen={this.props.isOpen} toggle={this.props.toggle} onOpened={this.handleOpen} size='lg'>
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