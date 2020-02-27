import * as React from 'react';
import styled from 'styled-components';
import { AircraftStatus } from '../Models';
import Panel from './Controls/Panel';

interface Props {
    aircrafts: { [connectionId: string]: AircraftStatus };
    onAircraftClick: (connectionId: string, aircraft: AircraftStatus) => void;

    myConnectionId: string | null;
    onMeChanged: (connectionId: string | null) => void;
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

    handleMeChanged(connectionId: string) {
        if (this.props.myConnectionId === connectionId) {
            this.props.onMeChanged(null);
        } else {
            this.props.onMeChanged(connectionId);
        }
    }

    public render() {
        const connectionIds = Object.keys(this.props.aircrafts);
        const list = connectionIds.length === 0 ?
            <tr><td colSpan={3}><NoneText>None</NoneText></td></tr> :
            connectionIds.map(connectionId => (
                <ListItem key={connectionId}>
                    <td>
                        <button className="btn btn-link" onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>
                            {this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}
                        </button>
                    </td>
                    <td><Checkbox type="checkbox" checked={this.props.myConnectionId === connectionId} onChange={() => this.handleMeChanged(connectionId)} /></td>
                    <td><Checkbox type="checkbox" checked={this.props.followingConnectionId === connectionId} onChange={() => this.handleFollowChanged(connectionId)} /></td>
                </ListItem>
            ));

        return <Wrapper>
            <List>
                <thead>
                    <tr>
                        <th><Title>Aircrafts</Title></th>
                        <th><div style={{ marginRight: 3, marginLeft: 3 }}>Me</div></th>
                        <th>Follow</th>
                    </tr>
                </thead>
                <tbody>
                    {list}
                </tbody>
            </List>
        </Wrapper>
    }
}

const Title = styled.div`
margin-left: 8px;
font-weight: bold;
font-style: italic;
text-align: center;
`

const NoneText = styled.div`
margin: 0 8px;
`

const Wrapper = styled(Panel)`
position: absolute;
top: 10px;
right: 10px;
z-index: 1000;
`

const List = styled.table`
margin-top: 10px;
list-style: none;
padding: 0;
`

const ListItem = styled.tr`
button {
display: block;
font-weight: bold;
width: 100%;
}
td {
text-align: center;
}
`

const Checkbox = styled.input`
text-align: center;
`