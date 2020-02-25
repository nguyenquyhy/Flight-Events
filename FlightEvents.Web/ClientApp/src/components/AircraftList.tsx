import * as React from 'react';
import styled from 'styled-components';
import { AircraftStatus } from '../Models';

interface Props {
    aircrafts: { [connectionId: string]: AircraftStatus };
    onAircraftClick: (connectionId: string, aircraft: AircraftStatus) => void;

    followingConnectionId: string | null;
    onFollowingChanged: (connectionId: string | null) => void;
}

export default class AircraftList extends React.Component<Props> {
    static displayName = AircraftList.name;

    handleFollowChanged(connectionId: string) {
        if (this.props.followingConnectionId === connectionId) {
            this.props.onFollowingChanged(null);
        } else {
            this.props.onFollowingChanged(connectionId);
        }
    }

    public render() {
        const list = Object.keys(this.props.aircrafts).map(connectionId => (
            <ListItem key={connectionId}>
                <a href="#" onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>{this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}</a>
                <label><input type="checkbox" checked={this.props.followingConnectionId == connectionId} onChange={() => this.handleFollowChanged(connectionId)} /> Follow</label>
            </ListItem>
        ));

        return <Wrapper>
            <strong><em>Aircrafts</em></strong>
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
a {
display: block;
font-weight: bold;
}
`