import * as React from 'react';
import { UncontrolledTooltip } from 'reactstrap';
import styled from 'styled-components';
import { AircraftStatus } from '../Models';
import Panel from './Controls/Panel';
import AircraftListItem from './AircraftListItem';
import Download from './Download';

interface Props {
    aircrafts: { [connectionId: string]: AircraftStatus };
    onAircraftClick: (connectionId: string) => void;

    myConnectionId: string | null;
    onMeChanged: (connectionId: string | null) => void;

    followingConnectionId: string | null;
    onFollowingChanged: (connectionId: string | null) => void;

    moreInfoConnectionIds: string[];
    onMoreInfoChanged: (connectionId: string) => void;

    flightPlanConnectionId: string | null;
    onFlightPlanChanged: (connectionId: string | null) => void;
}

export default class AircraftList extends React.Component<Props> {
    static displayName = AircraftList.name;

    public render() {
        let connectionIds = Object
            .entries(this.props.aircrafts)
            .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
            .map(o => o[0]);

        if (this.props.myConnectionId) {
            connectionIds = connectionIds.filter(o => o !== this.props.myConnectionId);
            connectionIds = [this.props.myConnectionId].concat(connectionIds);
        }
        const list = connectionIds.length === 0 ?
            <tr><td colSpan={4}><NoneText>None</NoneText></td></tr> :
            connectionIds.map(connectionId => (
                <AircraftListItem key={connectionId}
                    connectionId={connectionId}

                    callsign={this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}
                    onAircraftClick={this.props.onAircraftClick}

                    isReady={this.props.aircrafts[connectionId].isReady}

                    isMe={this.props.myConnectionId === connectionId}
                    onMeChanged={this.props.onMeChanged}
                    isFollowing={this.props.followingConnectionId === connectionId}
                    onFollowingChanged={this.props.onFollowingChanged}
                    isMoreInfo={this.props.moreInfoConnectionIds.includes(connectionId)}
                    onMoreInfoChanged={this.props.onMoreInfoChanged}
                    isFlightPlan={this.props.flightPlanConnectionId === connectionId}
                    onFlightPlanChanged={this.props.onFlightPlanChanged}
                />));

        return <Wrapper>
            <Download />
            <List>
                <thead>
                    <tr>
                        <th><Title>Aircraft {(connectionIds.length === 0 ? "" : `(${connectionIds.length})`)}</Title></th>
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
                        <th>
                            <div id="txtMore">Pln</div>
                            <UncontrolledTooltip placement="right" target="txtMore">Show flight plan</UncontrolledTooltip>
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