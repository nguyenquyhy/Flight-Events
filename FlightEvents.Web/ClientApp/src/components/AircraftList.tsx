import * as React from 'react';
import styled from 'styled-components';
import { AircraftStatus } from '../Models';
import Panel from './Controls/Panel';

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
        const connectionIds = Object.keys(this.props.aircrafts);
        const list = connectionIds.length === 0 ?
            <div><em>None</em></div> :
            connectionIds.map(connectionId => (
                <ListItem key={connectionId}>
                    <button className="btn btn-link" onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>{this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}</button>
                    <label><input type="checkbox" checked={this.props.followingConnectionId === connectionId} onChange={() => this.handleFollowChanged(connectionId)} /> Follow</label>
                </ListItem>
            ));

        return <Wrapper>
            <strong><em>Aircrafts</em></strong>
            <List>{list}</List>
        </Wrapper>
    }
}

const Wrapper = styled(Panel)`
position: absolute;
top: 10px;
right: 10px;
z-index: 10000;;
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