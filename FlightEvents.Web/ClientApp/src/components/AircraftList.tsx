import * as React from 'react';
import styled from 'styled-components';
import { AircraftStatus } from '../Models';

interface Props {
    aircrafts: { [connectionId: string]: AircraftStatus };
    onAircraftClick: (connectionId: string, aircraft: AircraftStatus) => void;
}

export default class AircraftList extends React.Component<Props> {
    static displayName = AircraftList.name;

    public render() {
        const list = Object.keys(this.props.aircrafts).map(connectionId => (
            <ListItem key={connectionId} onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>
                {this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}
            </ListItem>
        ));

        return <Wrapper>
            <strong>Aircrafts</strong>
            <List>{list}</List>
        </Wrapper>
    }
}

const Wrapper = styled.div`
position: absolute;
top: 0;
right: 0;
background-color: rgba(255, 255, 255, 0.9);
z-index: 10000;
padding: 10px 20px;
`

const List = styled.ul`
list-style: none;
padding: 0;
`

const ListItem = styled.li`
cursor: hand;
`