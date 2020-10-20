import * as React from 'react';
import styled from 'styled-components';
import { HubConnection } from '@microsoft/signalr';
import { Button, ButtonGroup, ListGroupItem } from 'reactstrap';
import { Stopwatch } from '../Models';

interface ItemProps {
    hub: HubConnection;
    eventId: string;
}

interface ItemState {
    elapsed: number;
}

function formatTime(elapsed: number) {
    return `${(Math.floor(elapsed / 1000 / 3600) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000 / 60) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000) % 60).toString().padStart(2, '0')}.${(elapsed % 1000).toString().padStart(3, '0')}`;
}

const StopwatchItem = (props: ItemProps & Stopwatch) => {
    const [state, setState] = React.useState<ItemState>({ elapsed: 0 });

    React.useEffect(() => {
        if (!props.stoppedDateTime && props.startedDateTime) {
            const start = new Date(props.startedDateTime).getTime();
            const interval = setInterval(() => {
                setState({ elapsed: new Date().getTime() - start - window['shift'] })
            }, 100);

            return () => {
                clearInterval(interval)
            }
        } else if (props.stoppedDateTime && props.startedDateTime) {
            setState({ elapsed: new Date(props.stoppedDateTime).getTime() - new Date(props.startedDateTime).getTime() })
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

    const handleReset = () => {
        props.hub.send("RestartStopwatch", props.eventId, props.id);
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
            <StyledTime>{formatTime(state.elapsed)}</StyledTime>
        </div>
        <div>
            <ButtonGroup>
                {!props.startedDateTime ? <Button color="primary" onClick={handleStart}>Start</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button onClick={handleLap}>Lap</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button color="warning" onClick={handleStop}>Stop</Button> : null}
                {props.stoppedDateTime ? <Button color="info" onClick={handleSave}>Save</Button> : null}
            </ButtonGroup>
            <ButtonGroup style={{ float: 'right' }}>
                {props.stoppedDateTime ? <Button onClick={handleReset}>Restart</Button> : null}
                {!props.startedDateTime || props.stoppedDateTime ? <Button color="danger" onClick={handleRemove}>Remove</Button> : null}
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