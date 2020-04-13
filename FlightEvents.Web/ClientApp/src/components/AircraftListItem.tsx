import * as React from 'react';
import styled from 'styled-components';

interface Props {
    connectionId: string;

    callsign: string;
    onAircraftClick: (connectionId: string) => void;

    isReady: boolean;

    isMe: boolean;
    onMeChanged: (connectionId: string | null) => void;
    isFollowing: boolean;
    onFollowingChanged: (connectionId: string | null) => void;
    isMoreInfo: boolean;
    onMoreInfoChanged: (connectionId: string) => void;
    isFlightPlan: boolean;
    onFlightPlanChanged: (connectionId: string | null) => void;
}

export default class AircraftListItem extends React.Component<Props> {
    static displayName = AircraftListItem.name;

    constructor(props: Props) {
        super(props);

        this.handleAircraftClick = this.handleAircraftClick.bind(this);
        this.handleMeChanged = this.handleMeChanged.bind(this);
        this.handleFollowChanged = this.handleFollowChanged.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
        this.handleFlightPlanChanged = this.handleFlightPlanChanged.bind(this);
    }

    handleAircraftClick() {
        this.props.onAircraftClick(this.props.connectionId)
    }

    handleMeChanged() {
        if (this.props.isMe) {
            this.props.onMeChanged(null);
        } else {
            this.props.onMeChanged(this.props.connectionId);
        }
    }

    handleFollowChanged() {
        if (this.props.isFollowing) {
            this.props.onFollowingChanged(null);
        } else {
            this.props.onFollowingChanged(this.props.connectionId);
        }
    }

    handleMoreInfoChanged() {
        this.props.onMoreInfoChanged(this.props.connectionId);
    }

    handleFlightPlanChanged() {
        if (this.props.isFlightPlan) {
            this.props.onFlightPlanChanged(null);
        } else {
            this.props.onFlightPlanChanged(this.props.connectionId);
        }
    }

    public render() {
        return <ListItem>
            <td>
                <button className="btn btn-link" disabled={!this.props.isReady} onClick={this.handleAircraftClick}>{this.props.callsign}</button>
            </td>
            <td><Checkbox type="checkbox" checked={this.props.isMe} onChange={this.handleMeChanged} /></td>
            <td><Checkbox type="checkbox" checked={this.props.isFollowing} onChange={this.handleFollowChanged} /></td>
            <td><Checkbox type="checkbox" checked={this.props.isMoreInfo} onChange={this.handleMoreInfoChanged} /></td>
            <td><Checkbox type="checkbox" checked={this.props.isFlightPlan} onChange={this.handleFlightPlanChanged} /></td>
        </ListItem>;
    }
}

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