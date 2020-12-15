import { HubConnection } from '@microsoft/signalr';
import * as React from 'react';
import { LeaderboardRecord, Stopwatch } from '../Models';

interface StopwatchHubProps {
    eventId: string;

    hub: HubConnection;
    onUpdateStopwatch: ((stopwatch: Stopwatch, serverDateString: string) => void) | null;
    onRemoveStopwatch: ((stopwatch: Stopwatch) => void) | null;
    onUpdateLeaderboard: (records: LeaderboardRecord[]) => void;
}

export default (props: StopwatchHubProps) => {
    React.useEffect(() => {
        const hub = props.hub;

        const f = async () => {
            if (props.onUpdateStopwatch) hub.on("UpdateStopwatch", props.onUpdateStopwatch);
            if (props.onRemoveStopwatch) hub.on("RemoveStopwatch", props.onRemoveStopwatch);
            hub.on("UpdateLeaderboard", props.onUpdateLeaderboard)
            await hub.start();
            await hub.send("Join", "Stopwatch:" + props.eventId);
        }
        f();

        return () => {
            hub.stop();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);//props.eventId, props.onUpdateStopwatch, props.onRemoveStopwatch, props.onUpdateLeaderboard])

    return <></>
}