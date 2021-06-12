import { HubConnection } from '@microsoft/signalr';
import * as React from 'react';
import { LeaderboardRecord, Stopwatch } from '../Models';

interface StopwatchHubProps {
    eventId: string;

    hub: HubConnection;
    onUpdateStopwatch: ((stopwatch: Stopwatch, serverDateString: string) => void) | null;
    onRemoveStopwatch: ((stopwatch: Stopwatch) => void) | null;
    onUpdateLeaderboard: (records: LeaderboardRecord[]) => void;

    onConnected?: () => void;
    onDisconnected?: () => void;
}

export default (props: StopwatchHubProps) => {
    React.useEffect(() => {
        const hub = props.hub;
        const f = async () => {
            if (props.onUpdateStopwatch) hub.on("UpdateStopwatch", props.onUpdateStopwatch);
            if (props.onRemoveStopwatch) hub.on("RemoveStopwatch", props.onRemoveStopwatch);
            if (props.onDisconnected) hub.onreconnecting(props.onDisconnected);
            hub.onreconnected(async () => {
                if (props.onConnected) props.onConnected();
                await hub.send("Join", "Stopwatch:" + props.eventId);
            });

            hub.on("UpdateLeaderboard", props.onUpdateLeaderboard)
            await hub.start();
            if (props.onConnected) props.onConnected();
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