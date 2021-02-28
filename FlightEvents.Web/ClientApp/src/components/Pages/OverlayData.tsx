import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { RouteComponentProps } from 'react-router-dom';
import { AircraftStatus } from '../../Models';

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub?clientType=Web')
    .withAutomaticReconnect()
    .build();

const OverlayData = (props: RouteComponentProps<{}>) => {
    const params = new URLSearchParams(props.location.search);
    const callsign = params.get('callsign');
    const parameter = params.get('parameter');

    const [value, setValue] = React.useState<any>(null);

    React.useEffect(() => {
        hub.on("UpdateAircraft", (clientId, aircraftStatus: AircraftStatus) => {
            if (aircraftStatus.callsign === callsign && parameter) {
                setValue(aircraftStatus[parameter]);
            }
        });

        const f = async () => {
            await hub.start();
        }
        f();

        return () => {
            hub.stop();
        }
    }, [callsign, parameter]);

    return <span className="value">{(value === null || value === undefined ? '-' : typeof (value) === 'number' ? (Math.round(value * 100) / 100).toString() : value.toString())}</span>
}

export default OverlayData;