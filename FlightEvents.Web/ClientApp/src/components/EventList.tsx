import * as React from 'react';
import styled from 'styled-components';
import Panel from './Controls/Panel';
import { FlightEvent, Airport } from '../Models';
import EventItem from './EventItem';
import Api from '../Api';

interface Props {
    onAirportsLoaded: (airports: Airport[]) => void;
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
            this.state.flightEvents.map(flightEvent => <EventItem key={flightEvent.id} flightEvent={flightEvent} onAirportsLoaded={this.props.onAirportsLoaded} />)

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