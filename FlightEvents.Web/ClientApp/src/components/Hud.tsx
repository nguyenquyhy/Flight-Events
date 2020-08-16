import * as React from 'react';
import styled from 'styled-components';
import { Input } from 'reactstrap';
import Download from './Download';
import { AircraftStatus } from '../Models';

interface Props {
    aircrafts: { [clientId: string]: AircraftStatus };
    onAircraftClick: (clientId: string) => void;

    myClientId: string | null;
    onMeChanged: (clientId: string | null) => void;

    followingClientId: string | null;
    onFollowingChanged: (clientId: string | null) => void;

    flightPlanClientId: string | null;
    onFlightPlanChanged: (clientId: string | null) => void;
}

export default (props: Props) => {
    let clientIds = Object
        .entries(props.aircrafts)
        .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
        .map(o => o[0]);

    const handleMeChanged = (event: React.ChangeEvent<HTMLSelectElement>) => {
        if (!event.target.value) {
            props.onMeChanged(null);
        } else {
            props.onMeChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    const handleFollowChanged = (event: React.ChangeEvent<HTMLSelectElement>) => {
        if (!event.target.value) {
            props.onFollowingChanged(null);
        } else {
            props.onFollowingChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    const handleFlightPlanChanged = (event: React.ChangeEvent<HTMLSelectElement>) => {
        if (!event.target.value) {
            props.onFlightPlanChanged(null);
        } else {
            props.onFlightPlanChanged(event.target.value);
            props.onAircraftClick(event.target.value);
        }
    }

    return <StyledWrapper>
        <Input type="select" name="ownAircraft" id="ownAircraft" defaultValue="" onChange={handleMeChanged}>
            <option value="">Select your aircraft</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId].callsign}</option>)}
        </Input>
        <Input type="select" name="followAircraft" id="followAircraft" defaultValue="" onChange={handleFollowChanged}>
            <option value="">Follow an aircraft</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId].callsign}</option>)}
        </Input>
        <Input type="select" name="flightplanAircraft" id="flightplanAircraft" defaultValue="" onChange={handleFlightPlanChanged}>
            <option value="">Show Flight Plan</option>
            {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId].callsign}</option>)}
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