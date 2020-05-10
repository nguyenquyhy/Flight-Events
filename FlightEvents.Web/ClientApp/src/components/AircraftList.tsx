import * as React from 'react';
import { UncontrolledTooltip } from 'reactstrap';
import styled from 'styled-components';
import { css } from 'styled-components';
import { AircraftStatus } from '../Models';
import Panel from './Controls/Panel';
import AircraftListItem from './AircraftListItem';
import Download from './Download';

interface Props {
    aircrafts: { [clientId: string]: AircraftStatus };
    onAircraftClick: (clientId: string) => void;

    myClientId: string | null;
    onMeChanged: (clientId: string | null) => void;

    showPathClientIds: string[];
    onShowPathChanged: (clientId: string) => void;

    followingClientId: string | null;
    onFollowingChanged: (clientId: string | null) => void;

    moreInfoClientIds: string[];
    onMoreInfoChanged: (clientId: string) => void;

    flightPlanClientId: string | null;
    onFlightPlanChanged: (clientId: string | null) => void;
}

interface State {
    collapsed: boolean;
}

export default class AircraftList extends React.Component<Props, State> {
    static displayName = AircraftList.name;

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
        let clientIds = Object
            .entries(this.props.aircrafts)
            .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
            .map(o => o[0]);

        if (this.props.myClientId) {
            clientIds = clientIds.filter(o => o !== this.props.myClientId);
            clientIds = [this.props.myClientId].concat(clientIds);
        }
        const list = clientIds.length === 0 ?
            <tr><td colSpan={4}><NoneText>None</NoneText></td></tr> :
            clientIds.map(clientId => (
                <AircraftListItem key={clientId}
                    clientId={clientId}

                    callsign={this.props.aircrafts[clientId].callsign || clientId.substring(5)}
                    onAircraftClick={this.props.onAircraftClick}

                    isReady={this.props.aircrafts[clientId].isReady}

                    isMe={this.props.myClientId === clientId}
                    onMeChanged={this.props.onMeChanged}
                    isShowPath={this.props.showPathClientIds.includes(clientId)}
                    onShowPathChanged={this.props.onShowPathChanged}
                    isFollowing={this.props.followingClientId === clientId}
                    onFollowingChanged={this.props.onFollowingChanged}
                    isMoreInfo={this.props.moreInfoClientIds.includes(clientId)}
                    onMoreInfoChanged={this.props.onMoreInfoChanged}
                    isFlightPlan={this.props.flightPlanClientId === clientId}
                    onFlightPlanChanged={this.props.onFlightPlanChanged}
                />));

        return <Wrapper collapsed={this.state.collapsed}>
            <Download />
            <ListWrapper>
                <List>
                    <thead>
                        <tr>
                            <th><Title>Aircraft {(clientIds.length === 0 ? "" : `(${clientIds.length})`)}</Title></th>
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
                            <th>
                                <div id="txtMore">Rte</div>
                                <UncontrolledTooltip placement="right" target="txtMore">Show flight route</UncontrolledTooltip>
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        {list}
                    </tbody>
                </List>
                <Button className="btn" onClick={this.handleToggle}><i className={"fas " + (this.state.collapsed ? "fa-chevron-left" : "fa-chevron-right")}></i></Button>
            </ListWrapper>
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

const Wrapper = styled<any>(Panel)`
position: absolute;
top: 10px;
right: 10px;
z-index: 1000;
max-height: calc(100% - 200px);
overflow-y: auto;

${props => props.collapsed && css`
width: 78px;
overflow-x: hidden;
`}
`

const ListWrapper = styled.div`
position: relative;
`

const List = styled.table`
margin-top: 10px;
margin-right: 5px;
margin-left: 8px;
list-style: none;
padding: 0;

th div {
min-width: 20px;
text-align: center;
}
`

const Button = styled.button`
position: absolute;
top: 0;
bottom: 0;
padding: 0;
width: 12px;

i {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    font-size: 8px;
}
`