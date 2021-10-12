import * as React from 'react';
import { Button, UncontrolledTooltip, Modal, ModalHeader, ModalBody } from 'reactstrap';
import styled from 'styled-components';
import { deepEqual } from '../Compare';
import AircraftListItem from './AircraftListItem';

export interface AircraftStatusInList {
    callsign: string;
    group: string | null;
    isReady: boolean;
}

interface Props {
    group: string | null;
    myClientId: string | null;
    followingClientId: string | null;
    flightPlanClientId: string | null;

    aircrafts: { [clientId: string]: AircraftStatusInList };
    onAircraftClick: (clientId: string) => void;

    showRouteClientIds: string[];
    onShowRouteChanged: (clientId: string) => void;

    moreInfoClientIds: string[];
    onMoreInfoChanged: (clientId: string) => void;
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

    shouldComponentUpdate(nextProps, nextState) {
        return this.state !== nextState || !deepEqual(this.props, nextProps);
    }

    handleToggle() {
        this.setState({
            collapsed: !this.state.collapsed
        })
    }

    renderAircrafList(clientIds: string[]) {
        if (this.props.myClientId && clientIds.includes(this.props.myClientId)) {
            // Move own aircraft to the top
            clientIds = clientIds.filter(o => o !== this.props.myClientId);
            clientIds = [this.props.myClientId].concat(clientIds);
        }
        return clientIds.length === 0 ?
            <tr><td colSpan={4}><NoneText>None</NoneText></td></tr> :
            clientIds.map(clientId => (
                <AircraftListItem key={clientId}
                    clientId={clientId}

                    callsign={this.props.aircrafts[clientId].callsign || clientId.substring(5)}
                    onAircraftClick={this.props.onAircraftClick}

                    isReady={this.props.aircrafts[clientId].isReady}

                    isMe={this.props.myClientId === clientId}
                    isFollowing={this.props.followingClientId === clientId}
                    isFlightPlan={this.props.flightPlanClientId === clientId}

                    isShowPath={this.props.showRouteClientIds.includes(clientId)}
                    onShowPathChanged={this.props.onShowRouteChanged}
                    isMoreInfo={this.props.moreInfoClientIds.includes(clientId)}
                    onMoreInfoChanged={this.props.onMoreInfoChanged}
                />));
    }

    public render() {
        let groupClientIds = this.props.group ? Object
            .entries(this.props.aircrafts)
            .filter(o => o[1].group === this.props.group)
            .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
            .map(o => o[0]) : [];

        let clientIds = Object
            .entries(this.props.aircrafts)
            .filter(o => !this.props.group || o[1].group !== this.props.group)
            .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
            .map(o => o[0]);

        const aircraftCount = this.props.group ? groupClientIds.length : clientIds.length;
        const buttonTitle = `Aircraft${(this.props.group ? '*' : '')} ${(aircraftCount ? `(${aircraftCount})` : '')}`;

        return <>
            <StyledButton color="secondary" onClick={this.handleToggle}>{buttonTitle}</StyledButton>
            <Modal isOpen={this.state.collapsed} toggle={this.handleToggle}>
                <ModalHeader>
                    Aircraft List {(clientIds.length === 0 ? "" : `(${clientIds.length})`)}
                </ModalHeader>
                <ModalBody>
                    {!!this.props.group && (
                        <>
                            <h6>Selected Group: <strong>{this.props.group}</strong></h6>

                            <List>
                                <thead>
                                    <tr>
                                        <th><Title>Callsign</Title></th>
                                        <th>
                                            <div id="txtMore">Show Info</div>
                                            <UncontrolledTooltip placement="right" target="txtMore">Show more info</UncontrolledTooltip>
                                        </th>
                                        <th>
                                            <div id="txtMore">Show Route</div>
                                            <UncontrolledTooltip placement="right" target="txtMore">Show flight route</UncontrolledTooltip>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {this.renderAircrafList(groupClientIds)}
                                </tbody>
                            </List>

                            <hr />
                            <h6>Other aircraft</h6>
                        </>
                    )}
                    <List>
                        <thead>
                            <tr>
                                <th><Title>Callsign</Title></th>
                                <th>
                                    <div id="txtMore">Show Info</div>
                                    <UncontrolledTooltip placement="right" target="txtMore">Show more info</UncontrolledTooltip>
                                </th>
                                <th>
                                    <div id="txtMore">Show Route</div>
                                    <UncontrolledTooltip placement="right" target="txtMore">Show flight route</UncontrolledTooltip>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {this.renderAircrafList(clientIds)}
                        </tbody>
                    </List>
                </ModalBody>
            </Modal>
        </>
    }
}

const StyledButton = styled(Button)`
position: fixed;
top: 10px;
right: 150px;
width: 130px;
z-index: 1000;
`

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