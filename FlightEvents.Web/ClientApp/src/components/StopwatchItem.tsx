import * as React from 'react';
import styled from 'styled-components';
import { HubConnection } from '@microsoft/signalr';
import { Button, ButtonGroup, ListGroupItem } from 'reactstrap';
import { Stopwatch } from '../Models';

interface ItemProps {
    hub: HubConnection;
    eventId: string;
    removed: boolean;
}

function formatTime(elapsed: number) {
    return `${(Math.floor(elapsed / 1000 / 3600) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000 / 60) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000) % 60).toString().padStart(2, '0')}.${(elapsed % 1000).toString().padStart(3, '0')}`;
}

const StopwatchItem = (props: ItemProps & Stopwatch) => {
    const [elapsed, setElapsed] = React.useState<number>(0);

    React.useEffect(() => {
        if (!props.stoppedDateTime && props.startedDateTime) {
            const start = new Date(props.startedDateTime).getTime();
            const interval = setInterval(() => {
                setElapsed(new Date().getTime() - start - window['shift'])
            }, 100);

            return () => {
                clearInterval(interval)
            }
        } else if (props.stoppedDateTime && props.startedDateTime) {
            setElapsed(new Date(props.stoppedDateTime).getTime() - new Date(props.startedDateTime).getTime())
        }
    }, [props])

    const handleStart = () => {
        props.hub.send("StartStopwatch", props.eventId, props.id);
    }

    const handleLap = () => {
        props.hub.send("LapStopwatch", props.eventId, props.id);
    }

    const handleStop = () => {
        props.hub.send("StopStopwatch", props.eventId, props.id);
    }

    const handleRestart = () => {
        props.hub.send("RestartStopwatch", props.eventId, props.id);
    }

    const handleReset = () => {
        props.hub.send("ResetStopwatch", props.eventId, props.id);
    }

    const handleSave = () => {
        props.hub.send("SaveStopwatch", props.eventId, props.id);
    }

    const handleRemove = () => {
        props.hub.send("RemoveStopwatch", props.eventId, props.id);
    }

    const startedDateTime = props.startedDateTime ? new Date(props.startedDateTime).getTime() : null;

    return <ListGroupItem>
        <div>{props.name} ({props.leaderboardName})</div>
        <div>
            <StyledTime>{props.removed ? "Removed" : formatTime(elapsed)}</StyledTime>
        </div>
        <div>
            <ButtonGroup>
                {!props.startedDateTime ? <Button color="primary" onClick={handleStart} disabled={props.removed}>Start</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button onClick={handleLap} disabled={props.removed}>Lap</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button color="warning" onClick={handleStop} disabled={props.removed}>Stop</Button> : null}
                {props.stoppedDateTime ? <Button color="info" onClick={handleSave} disabled={props.removed}>Save</Button> : null}
            </ButtonGroup>
            <ButtonGroup style={{ float: 'right' }}>
                {props.stoppedDateTime ? <Button onClick={handleRestart} disabled={props.removed}>Restart</Button> : null}
                {props.stoppedDateTime ? <Button onClick={handleReset} disabled={props.removed}>Reset</Button> : null}
                {!props.startedDateTime || props.stoppedDateTime ? <Button color="danger" onClick={handleRemove} disabled={props.removed}>Remove</Button> : null}
            </ButtonGroup>
        </div>

        <StyledLaps>
            {startedDateTime ? props.lapsDateTime.map((lapDateTime, index) => <li key={index}>{formatTime(new Date(lapDateTime).getTime() - new Date(startedDateTime).getTime())}</li>) : null}
        </StyledLaps>
    </ListGroupItem>;
}

const StyledTime = styled.span`
font-size: 2em;
font-family: Courier New,Courier,Lucida Sans Typewriter,Lucida Typewriter,monospace;
position: relative;
`

const StyledLaps = styled.ul`
position: absolute;
top: 10px;
right: 10px;
font-family: Courier New,Courier,Lucida Sans Typewriter,Lucida Typewriter,monospace;
font-weight: bold;
`

export default StopwatchItem;