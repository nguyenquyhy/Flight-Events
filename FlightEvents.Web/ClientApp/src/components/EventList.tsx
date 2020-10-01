import * as React from 'react';
import styled, { css } from 'styled-components';
import parseJSON from 'date-fns/parseJSON';
import compareDesc from 'date-fns/compareDesc';
import isBefore from 'date-fns/isBefore';
import addHours from 'date-fns/addHours';
import { HubConnection } from '@microsoft/signalr';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import Panel from './Controls/Panel';
import { FlightEvent, Airport, FlightPlan } from '../Models';
import EventItem from './EventItem';
import PastEvents from './PastEvents';

interface Props {
    hub: HubConnection;
    onAirportsLoaded: (airports: Airport[]) => void;
    onFlightPlansLoaded: (flightPlans: FlightPlan[]) => void;
}

interface State {
    collapsed: boolean;
}

export default class EventList extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            collapsed: false
        }

        this.handleToggle = this.handleToggle.bind(this);
    }

    handleToggle() {
        this.setState({
            collapsed: !this.state.collapsed
        })
    }

    public render() {
        return <Query query={gql`{
    flightEvents {
        id
        type
        name
        startDateTime
    }
}`}>{({ loading, error, data }: ApolloQueryResult<{ flightEvents: FlightEvent[] }>) => {
                if (loading) return <Wrapper collapsed={this.state.collapsed}>Loading...</Wrapper>;
                if (error) return <Wrapper collapsed={this.state.collapsed}>Error</Wrapper>;

                const upcoming = data.flightEvents
                    .filter(ev => isBefore(new Date(), addHours(parseJSON(ev.startDateTime), ev.type === 'RACE' ? 24 : 4)))
                    .sort((a, b) => compareDesc(parseJSON(b.startDateTime), parseJSON(a.startDateTime)));

                const list = data.flightEvents.length === 0 ?
                    <NoneText>None</NoneText> :
                    upcoming.map(flightEvent => <EventItem key={flightEvent.id} hub={this.props.hub} flightEvent={flightEvent} onAirportsLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />)

                return <Wrapper collapsed={this.state.collapsed}>
                    <Button className="btn" onClick={this.handleToggle}><i className={"fas " + (this.state.collapsed ? "fa-chevron-up" : "fa-chevron-down")}></i></Button>
                    <Title collapsed={this.state.collapsed}>{upcoming.length === 0 ? "No upcoming events" : `Upcoming Events (${upcoming.length})`}</Title>
                    <List>{list}</List>
                    <PastEvents hub={this.props.hub} onAirportsLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />
                </Wrapper>
            }}
        </Query>
    }
}

const Wrapper = styled<any>(Panel)`
position: absolute;
bottom: 12px;
right: 10px;
z-index: 1000;

${props => props.collapsed && css`
height:30px;
overflow-y: hidden;
`}
`

const Title = styled.div<any>`
margin: 10px 11px 0px 11px;
font-weight: bold;
/*font-style: italic;*/
${props => props.collapsed && css`
margin: 2px 10px 0px 10px;
`}
`

const NoneText = styled.div`
margin: 0 10px;
`

const List = styled.ul`
list-style: none;
padding: 0;
margin-bottom: 4px;
`

const Button = styled.button`
padding: 0;
font-size: 8px;
display: block;
position: absolute;
left: 0;
right: 0;
top: 0;
width: 100%;
height: 12px;
`