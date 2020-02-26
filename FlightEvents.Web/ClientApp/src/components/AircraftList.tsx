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
            <NoneText>None</NoneText> :
            connectionIds.map(connectionId => (
                <ListItem key={connectionId}>
                    <button className="btn btn-link" onClick={() => this.props.onAircraftClick(connectionId, this.props.aircrafts[connectionId])}>
                        {this.props.aircrafts[connectionId].callsign || connectionId.substring(5)}
                    </button>
                    <CheckWrapper><input type="checkbox" checked={this.props.myConnectionId === connectionId} onChange={() => this.handleMeChanged(connectionId)} /> Me</CheckWrapper>
                    <CheckWrapper><input type="checkbox" checked={this.props.followingConnectionId === connectionId} onChange={() => this.handleFollowChanged(connectionId)} /> Follow</CheckWrapper>
                </ListItem>
            ));

        return <Wrapper>
            <Title>Aircrafts</Title>
            <List>{list}</List>
        </Wrapper>
    }
}

const Title = styled.div`
margin: 10px 10px 0px 10px;
font-weight: bold;
font-style: italic;
text-align: center;
`

const NoneText = styled.div`
margin: 0 10px;
`

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
button {
display: block;
font-weight: bold;
width: 100%;
}
`

const CheckWrapper = styled.label`
margin: 0 10px;
`