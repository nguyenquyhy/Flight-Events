import * as React from 'react';
import styled from 'styled-components';
import { Input } from 'reactstrap';
import { propsShallowEqual } from '../Compare';
import Download from './Download';

interface Props {
    aircrafts: { [clientId: string]: string };
    onAircraftClick: (clientId: string) => void;

    myClientId: string | null;
    onMeChanged: (clientId: string | null) => void;

    followingClientId: string | null;
    onFollowingChanged: (clientId: string | null) => void;

    flightPlanClientId: string | null;
    onFlightPlanChanged: (clientId: string | null) => void;
}

const Hud = (props: Props) => {
    let clientIds = Object
        .entries(props.aircrafts)
        .sort((a, b) => (a[1] || a[0].substring(5)).localeCompare((b[1] || b[0].substring(5))))
        .map(o => o[0]);

    const handleMeChanged = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (!event.target.value) {
            props.onMeChanged(null);
        } else {
            props.onMeChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    const handleFollowChanged = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (!event.target.value) {
            props.onFollowingChanged(null);
        } else {
            props.onFollowingChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    const handleFlightPlanChanged = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (!event.target.value) {
            props.onFlightPlanChanged(null);
        } else {
            props.onFlightPlanChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    return <StyledWrapper>
        <Input type="select" name="ownAircraft" id="ownAircraft" value={props.myClientId ?? ''} onChange={handleMeChanged}>
            <option value="">Select your aircraft</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
        </Input>
        <Input type="select" name="followAircraft" id="followAircraft" value={props.followingClientId ?? ''} onChange={handleFollowChanged}>
            <option value="">Follow an aircraft</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
        </Input>
        <Input type="select" name="flightplanAircraft" id="flightplanAircraft" value={props.flightPlanClientId ?? ''} onChange={handleFlightPlanChanged}>
            <option value="">Show Flight Plan</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
        </Input>
        <div></div>
        <Download />
    </StyledWrapper>
}

const StyledWrapper = styled.div`
position: fixed;
top: 0px;
left: 0px;
right: 0px;
z-index: 1000;
background-color: rgba(255, 255, 255, 0);
padding: 10px 10px 10px 44px;

display: grid;
grid-template-columns: auto auto auto 1fr auto;
`;

export default React.memo(Hud, propsShallowEqual);