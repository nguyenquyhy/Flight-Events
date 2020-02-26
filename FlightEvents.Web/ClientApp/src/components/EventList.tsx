import * as React from 'react';
import styled from 'styled-components';
import Panel from './Controls/Panel';
import { FlightEvent } from '../Models';
import EventItem from './EventItem';

interface State {
    flightEvents: FlightEvent[]
}

export default class EventList extends React.Component<any, State> {
    constructor(props: any) {
        super(props);

        this.state = {
            flightEvents: []
        }
    }

    componentDidMount() {
        this.populateData();
    }

    private async populateData() {
        const response = await fetch('graphql', {
            method: 'post',
            body: JSON.stringify({
                query: `{
    flightEvents {
        id
        name
    }
}`
            }),
            headers: {
                'Content-Type': 'application/json'
            }
        });


        if (response.ok) {
            const data = await response.json();
            const events = data.data.flightEvents as FlightEvent[];

            this.setState({
                flightEvents: events
            });
        } else {
            // TODO:
        }
    }

    public render() {
        const list = this.state.flightEvents.length === 0 ?
            <NoneText>None</NoneText> :
            this.state.flightEvents.map(flightEvent => <EventItem key={flightEvent.id} flightEvent={flightEvent} />)

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
z-index: 10000;
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