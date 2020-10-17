import * as React from 'react';
import styled from 'styled-components';
import { css } from 'styled-components';
import { FlightEvent, Airport, FlightPlan, FlightPlanWaypoint } from '../Models';
import EventModal from './EventModal';
import parseJSON from 'date-fns/parseJSON';
import addHours from 'date-fns/addHours';
import formatRelative from 'date-fns/formatRelative';
import isBefore from 'date-fns/isBefore';
import { HubConnection } from '@microsoft/signalr';

interface Props {
    hub: HubConnection;
    flightEvent: FlightEvent;
    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
    onCheckpointsLoaded: (checkpoints: FlightPlanWaypoint[]) => void;
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
            <CustomButton className={"btn btn-link"}
                endDateTime={this.props.flightEvent.endDateTime ? parseJSON(this.props.flightEvent.endDateTime) : addHours(parseJSON(this.props.flightEvent.startDateTime), 4)}
                onClick={this.handleToggle}>
                <EventTitle>{this.props.flightEvent.name}</EventTitle>
                <EventSubtitle>
                    ({formatRelative(parseJSON(this.props.flightEvent.startDateTime), new Date())})
                </EventSubtitle>
            </CustomButton>
            <EventModal hub={this.props.hub}
                isOpen={this.state.isOpen} toggle={this.handleToggle} flightEvent={this.props.flightEvent}
                onAirportLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} onCheckpointsLoaded={this.props.onCheckpointsLoaded} />
        </ListItem>
    }
}

const ListItem = styled.li`

`

const EventTitle = styled.h3`
font-size: 1.1em;
font-weight: semi-bold;
margin-bottom: 0;
`

const EventSubtitle = styled.div`
font-size: 0.9em;
text-align: right;
`

const CustomButton = styled.button<{ endDateTime: Date }>`
width: 100%;
text-align: left;
${props => isBefore(props.endDateTime, new Date()) && css`
color: gray;
`}
:hover, :focus {
    text-decoration: none;

    ${EventTitle} {
        text-decoration: underline;
    }
}
`