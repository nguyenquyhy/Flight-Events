import * as React from 'react';
import styled from 'styled-components';
import { FlightEvent } from '../Models';

interface Props {
    flightEvent: FlightEvent;
}

export default class EventItem extends React.Component<Props> {
    public render() {
        return <ListItem>
            <button className="btn btn-link">{this.props.flightEvent.name}</button>
        </ListItem>
    }
}

const ListItem = styled.li`

`