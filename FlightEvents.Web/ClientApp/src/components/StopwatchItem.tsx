import * as React from 'react';
import styled from 'styled-components';
import { HubConnection } from '@microsoft/signalr';
import { Button, ButtonGroup, Input, InputGroup, InputGroupAddon, ListGroupItem } from 'reactstrap';
import { Stopwatch } from '../Models';

interface ItemProps {
    hub: HubConnection;
    connected: boolean;

    eventId: string;
    removed: boolean;

    onRemarksChanged: (string) => void;
}

function formatTime(elapsed: number) {
    return `${(Math.floor(elapsed / 1000 / 3600) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000 / 60) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000) % 60).toString().padStart(2, '0')}.${(elapsed % 1000).toString().padStart(3, '0')}`;
}

const StopwatchItem = (props: ItemProps & Stopwatch) => {
    const [elapsed, setElapsed] = React.useState<number>(0);
    const [editingRemarks, setEditingRemarks] = React.useState<string | null>(null);

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

    const handleClickRemark = e => {
        setEditingRemarks(props.remarks || "");
    }

    const handleChangeRemarks = e => {
        setEditingRemarks(e.target.value);
    }

    const handleSaveRemarks = e => {
        props.onRemarksChanged(editingRemarks);
        setEditingRemarks(null);
    }

    const handleCancelRemark = e => {
        setEditingRemarks(null);
    }

    return <ListGroupItem>
        <div style={{ float: 'left', marginRight: 5 }}>{props.name} ({props.leaderboardName})</div>
        {!props.removed && editingRemarks !== null ?
            <RemarkEditWrapper size="sm">
                <Input value={editingRemarks} onChange={handleChangeRemarks} />
                <InputGroupAddon addonType="append">
                    <Button color="primary" onClick={handleSaveRemarks} disabled={!props.connected}>Save</Button>
                    <Button onClick={handleCancelRemark}>Cancel</Button>
                </InputGroupAddon>
            </RemarkEditWrapper> :
            <RemarkText onClick={handleClickRemark}>{props.remarks || "No remarks"}</RemarkText>}
        <div style={{ clear: 'both' }}></div>
        <div>
            <StyledTime>{props.removed ? "Removed" : formatTime(elapsed)}</StyledTime>
        </div>
        <div>
            <ButtonGroup>
                {!props.startedDateTime ? <Button color="primary" onClick={handleStart} disabled={!props.connected || props.removed}>Start</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button onClick={handleLap} disabled={!props.connected || props.removed}>Lap</Button> : null}
                {props.startedDateTime && !props.stoppedDateTime ? <Button color="warning" onClick={handleStop} disabled={!props.connected || props.removed}>Stop</Button> : null}
                {props.stoppedDateTime ? <Button color="info" onClick={handleSave} disabled={!props.connected || props.removed}>Save</Button> : null}
            </ButtonGroup>
            <ButtonGroup style={{ float: 'right' }}>
                {props.stoppedDateTime ? <Button onClick={handleRestart} disabled={!props.connected || props.removed}>Restart</Button> : null}
                {props.stoppedDateTime ? <Button onClick={handleReset} disabled={!props.connected || props.removed}>Reset</Button> : null}
                {!props.startedDateTime || props.stoppedDateTime ? <Button color="danger" onClick={handleRemove} disabled={!props.connected || props.removed}>Remove</Button> : null}
            </ButtonGroup>
        </div>

        <StyledLaps>
            {startedDateTime ? props.lapsDateTime.map((lapDateTime, index) => <li key={index}>{formatTime(new Date(lapDateTime).getTime() - new Date(startedDateTime).getTime())}</li>) : null}
        </StyledLaps>
    </ListGroupItem>;
}

const RemarkEditWrapper = styled(InputGroup)`
margin-top: -3px;
margin-bottom: -4px;
float: left;
width: 400px;
`

const RemarkText = styled.em`
float: left;
`

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