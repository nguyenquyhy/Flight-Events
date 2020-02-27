import * as React from 'react';
import styled from 'styled-components';
import { FlightEvent, Airport } from '../Models';
import EventModal from './EventModal';

interface Props {
    flightEvent: FlightEvent;
    onAirportsLoaded: (airports: Airport[]) => void;
}

interface State {
    isOpen: boolean;
}

export default class EventItem extends React.Component<Props, State> {
    constructor(props: any) {
        super(props);

        this.state = {
            isOpen: false
        };

        this.handleToggle = this.handleToggle.bind(this);
    }

    private handleToggle() {
        this.setState({
            isOpen: !this.state.isOpen
        })
    }

    public render() {
        return <ListItem>
            <button className="btn btn-link" onClick={this.handleToggle}>{this.props.flightEvent.name}</button>
            <EventModal isOpen={this.state.isOpen} toggle={this.handleToggle} flightEvent={this.props.flightEvent} onAirportLoaded={this.props.onAirportsLoaded} />
        </ListItem>
    }
}

const ListItem = styled.li`

`