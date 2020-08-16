import * as React from 'react';
import styled from 'styled-components';

interface Props {
    clientId: string;

    callsign: string;
    onAircraftClick: (clientId: string) => void;

    isReady: boolean;

    isMe: boolean;
    isFollowing: boolean;
    isFlightPlan: boolean;

    isShowPath: boolean;
    onShowPathChanged: (clientId: string) => void;
    isMoreInfo: boolean;
    onMoreInfoChanged: (clientId: string) => void;
}

export default class AircraftListItem extends React.Component<Props> {
    static displayName = AircraftListItem.name;

    constructor(props: Props) {
        super(props);

        this.handleAircraftClick = this.handleAircraftClick.bind(this);
        this.handleShowPathChanged = this.handleShowPathChanged.bind(this);
        this.handleMoreInfoChanged = this.handleMoreInfoChanged.bind(this);
    }

    handleAircraftClick() {
        this.props.onAircraftClick(this.props.clientId)
    }

    handleShowPathChanged() {
        this.props.onShowPathChanged(this.props.clientId);
    }

    handleMoreInfoChanged() {
        this.props.onMoreInfoChanged(this.props.clientId);
    }

    public render() {
        return <ListItem>
            <td>
                <button className="btn btn-link" disabled={!this.props.isReady} onClick={this.handleAircraftClick}>{this.props.callsign}</button>
            </td>
            <td><Checkbox type="checkbox" checked={this.props.isMoreInfo} onChange={this.handleMoreInfoChanged} /></td>
            <td><Checkbox type="checkbox" checked={this.props.isShowPath} onChange={this.handleShowPathChanged} /></td>
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