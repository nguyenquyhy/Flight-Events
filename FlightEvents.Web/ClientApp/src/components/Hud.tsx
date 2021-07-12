import * as React from 'react';
import styled from 'styled-components';
import { InputGroup, InputGroupAddon, Input, Button } from 'reactstrap';
import { propsShallowEqual } from '../Compare';
import Download from './Download';

interface Props {
    mode: string | null;

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

    const handleMoveToMe = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (props.myClientId) {
            props.onAircraftClick(props.myClientId);
        }
    }

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
        <InputGroup>
            {props.mode !== "MSFS" &&
                <>
                    <InputGroupAddon addonType="prepend">
                        <StyledButton outline color="secondary" disabled={!props.myClientId || !props.aircrafts[props.myClientId]} onClick={handleMoveToMe} title="Move to your aircraft" >
                            <svg xmlns="http://www.w3.org/2000/svg" height="34" width="34" viewBox="0 0 144 144">
                                <g>
                                    <path id="path1" transform="rotate(0,72,72) translate(32,32) scale(2.5,2.5)  " fill="#000000" d="M16,10.24C19.181,10.24 21.759999,12.819 21.759999,16 21.759999,19.181 19.181,21.76 16,21.76 12.819,21.76 10.24,19.181 10.24,16 10.24,12.819 12.819,10.24 16,10.24z M1.2799997,1.2799997L1.2799997,30.719999 30.719999,30.719999 30.719999,1.2799997z M0,0L32,0 32,32 0,32z" />
                                </g>
                            </svg>
                        </StyledButton>
                    </InputGroupAddon>
                    <Input type="select" name="ownAircraft" id="ownAircraft" value={props.myClientId ?? ''} onChange={handleMeChanged} title="Select your aircraft">
                        <option value="">Select your aircraft</option>
                        {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
                    </Input>
                </>
            }
            {props.mode !== "MSFS" &&
                <Input type="select" name="followAircraft" id="followAircraft" value={props.followingClientId ?? ''} onChange={handleFollowChanged} title="Select an aircraft to follow">
                    <option value="">Follow an aircraft</option>
                    {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
                </Input>
            }
            {props.mode !== "MSFS" &&
                <Input type="select" name="flightplanAircraft" id="flightplanAircraft" value={props.flightPlanClientId ?? ''} onChange={handleFlightPlanChanged} title="Select an aircraft to show flight plan">
                    <option value="">Show Flight Plan</option>
                    {clientIds.map(clientId => <option key={clientId} value={clientId}>{props.aircrafts[clientId]}</option>)}
                </Input>
            }
        </InputGroup>
        <div></div>
        <Download />
    </StyledWrapper>
}

const StyledWrapper = styled.div`
position: fixed;
top: 0px;
left: 0px;
right: 0px;
z-index: 900;
background-color: rgba(255, 255, 255, 0);
padding: 10px 10px 10px 48px;

display: grid;
grid-template-columns: auto 1fr auto;
`;

const StyledButton = styled(Button)`
padding: 0;
background-color: white;
`

export default React.memo(Hud, propsShallowEqual);