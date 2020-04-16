import * as React from 'react';
import styled from 'styled-components';
import { css } from 'styled-components';
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
    collapsed: boolean;
    flightEvents: FlightEvent[]
}

export default class EventList extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            collapsed: false,
            flightEvents: []
        }

        this.handleToggle = this.handleToggle.bind(this);
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

    handleToggle() {
        this.setState({
            collapsed: !this.state.collapsed
        })
    }

    public render() {
        const list = this.state.flightEvents.length === 0 ?
            <NoneText>None</NoneText> :
            this.state.flightEvents
                .sort((a, b) => compareDesc(parseJSON(a.startDateTime), parseJSON(b.startDateTime)))
                .map(flightEvent => <EventItem key={flightEvent.id} flightEvent={flightEvent} onAirportsLoaded={this.props.onAirportsLoaded} onFlightPlansLoaded={this.props.onFlightPlansLoaded} />)

        return <Wrapper collapsed={this.state.collapsed}>
            <Button className="btn" onClick={this.handleToggle}><i className={"fas " + (this.state.collapsed ? "fa-chevron-up" : "fa-chevron-down")}></i></Button>
            <Title>Events</Title>
            <List>{list}</List>
        </Wrapper>
    }
}

const Wrapper = styled<any>(Panel)`
position: absolute;
bottom: 24px;
right: 10px;
z-index: 1000;

${props => props.collapsed && css`
height:12px;
overflow-y: hidden;
`}
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