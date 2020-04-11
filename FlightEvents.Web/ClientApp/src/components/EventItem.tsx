import * as React from 'react';
import styled from 'styled-components';
import { css } from 'styled-components';
import { FlightEvent, Airport, FlightPlan } from '../Models';
import EventModal from './EventModal';
import parseJSON from 'date-fns/parseJSON';
import addHours from 'date-fns/addHours';
import formatRelative from 'date-fns/formatRelative';
import isBefore from 'date-fns/isBefore';

interface Props {
    flightEvent: FlightEvent;
    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
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
            <CustomButton className={"btn btn-link"} endDateTime={addHours(parseJSON(this.props.flightEvent.startDateTime), 4)} onClick={this.handleToggle}>{this.props.flightEvent.name} ({formatRelative(parseJSON(this.props.flightEvent.startDateTime), new Date())})</CustomButton>
            <EventModal isOpen={this.state.isOpen} toggle={this.handleToggle} flightEvent={this.props.flightEvent} onAirportLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />
        </ListItem>
    }
}

const ListItem = styled.li`

`

const CustomButton = styled.button`
${props => isBefore(props.endDateTime, new Date()) && css`display: none`}
`