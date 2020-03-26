import * as React from 'react';
import styled from 'styled-components';
import Panel from './Controls/Panel';
import { FlightEvent, Airport, FlightPlan } from '../Models';
import EventItem from './EventItem';
import Api from '../Api';
import parseJSON from 'date-fns/parseJSON';
import compareDesc from 'date-fns/compareDesc';

interface Props {
    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
}

interface State {
    flightEvents: FlightEvent[]
}

export default class EventList extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            flightEvents: []
        }
    }

    componentDidMount() {
        this.populateData();
    }

    private async populateData() {
        const events = await Api.getFlightEvents();

        this.setState({
            flightEvents: events
        });
    }

    public render() {
        const list = this.state.flightEvents.length === 0 ?
            <NoneText>None</NoneText> :
            this.state.flightEvents
                .sort((a, b) => compareDesc(parseJSON(a.startDateTime), parseJSON(b.startDateTime)))
                .map(flightEvent => <EventItem key={flightEvent.id} flightEvent={flightEvent} onAirportsLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />)

        return <Wrapper>
            <Title>Events</Title>
            <List>{list}</List>
        </Wrapper>
    }
}

const Wrapper = styled(Panel)`
position: absolute;
bottom: 24px;
right: 10px;
z-index: 1000;
`

const Title = styled.div`
margin: 10px 10px 0px 10px;
font-weight: bold;
font-style: italic;
`

const NoneText = styled.div`
margin: 0 10px;
`

const List = styled.ul`
list-style: none;
padding: 0;
`