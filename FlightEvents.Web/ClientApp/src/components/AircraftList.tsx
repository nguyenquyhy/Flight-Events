import * as React from 'react';
import { UncontrolledTooltip } from 'reactstrap';
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

    moreInfoConnectionIds: string[];
    onMoreInfoChanged: (connectionId: string) => void;
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

    handleMoreInfoChanged(connectionId: string) {
        this.props.onMoreInfoChanged(connectionId);
    }

    public render() {
        const connectionIds = Object.keys(this.props.aircrafts);
        const list = connectionIds.length === 0 ?
            <tr><td colSpan={4}><NoneText>None</NoneText></td></tr> :
            connectionIds.map(connectionId => (
                <ListItem key={connectionId}>
                    <td>
                        <button className="btn btn-link" onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>
                            {this.props.aircrafts[connectionId].Callsign || connectionId.substring(5)}
                        </button>
                    </td>
                    <td><Checkbox type="checkbox" checked={this.props.myConnectionId === connectionId} onChange={() => this.handleMeChanged(connectionId)} /></td>
                    <td><Checkbox type="checkbox" checked={this.props.followingConnectionId === connectionId} onChange={() => this.handleFollowChanged(connectionId)} /></td>
                    <td><Checkbox type="checkbox" checked={this.props.moreInfoConnectionIds.includes(connectionId)} onChange = {() => this.handleMoreInfoChanged(connectionId)} /></td>
                </ListItem>
            ));

        return <Wrapper>
            <List>
                <thead>
                    <tr>
                        <th><Title>Aircrafts</Title></th>
                        <th>
                            <div id="txtMe">Own</div>
                            <UncontrolledTooltip placement="right" target="txtMe">Own aircraft. Will display the visible range circle for multiplayer</UncontrolledTooltip>
                        </th>
                        <th>
                            <div id="txtFollow">Flw</div>
                            <UncontrolledTooltip placement="right" target="txtFollow">Keep the map centered on this aircraft</UncontrolledTooltip>
                        </th>
                        <th>
                            <div id="txtMore">Nfo</div>
                            <UncontrolledTooltip placement="right" target="txtMore">Show more info</UncontrolledTooltip>
                        </th>
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
margin-right: 8px;
font-weight: bold;
font-style: italic;
text-align: center;
`

const NoneText = styled.div`
margin: 0 8px 10px 8px;
`

const Wrapper = styled(Panel)`
position: absolute;
top: 10px;
right: 10px;
z-index: 1000;
max-height: calc(100% - 200px);
overflow-y: auto;
`

const List = styled.table`
margin-top: 10px;
margin-right: 5px;
list-style: none;
padding: 0;

th div {
min-width: 20px;
text-align: center;
}
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