import * as React from "react";
import { HubConnectionBuilder } from '@microsoft/signalr';

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

const AdminStopwatchesPage = () => {
    const [connected, setConnected] = React.useState(false);
    const [dataString, setDataString] = React.useState("");

    const handleStopwatchesReturned = (stopwatches) => {
        console.log(stopwatches);
        setDataString(JSON.stringify(stopwatches));
    }

    const handleChange = (e) => {
        setDataString(e.target.value);
    }

    const handleRequest = async () => {
        await hub.send('GetStopwatches');
    }

    const handleSet = async () => {
        await hub.send('AddStopwatches', JSON.parse(dataString));
    }

    React.useEffect(() => {
        const f = async () => {
            hub.onreconnecting(() => setConnected(false));
            hub.onreconnected(() => setConnected(true));
            hub.on("ReturnStopwatches", handleStopwatchesReturned);
            await hub.start();
            setConnected(true);
        }
        f();

        return () => {
            hub.stop();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);//props.eventId, props.onUpdateStopwatch, props.onRemoveStopwatch, props.onUpdateLeaderboard])

    return <>
        {connected ? <div><button onClick={handleRequest}>Request</button><button onClick={handleSet}>Set</button></div> : <>Connecting...</>}
        <textarea onChange={handleChange} value={dataString} style={{ minWidth: 600, minHeight: 400 }} />
    </>
}

export default AdminStopwatchesPage;