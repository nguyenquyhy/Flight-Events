import * as React from 'react';
import styled from 'styled-components';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { AircraftStatus, AircraftData } from '../../Models';
import { convertPropertyNames, pascalCaseToCamelCase } from '../../Converters';

const columnsSelected = {
    clientVersion: 'Version',
    simRate: 'Rate',
    latitude: 'Lat',
    longitude: 'Lon',
    pitch: 'Pitch',
    bank: 'Bank',
    heading: 'HDG',
    trueHeading: 'True HDG',
    altitude: 'MSL',
    altitudeAboveGround: 'AGL',
    groundSpeed: 'G/S',
    indicatedAirSpeed: 'IAS',
    trueAirSpeed: 'TAS',
    verticalSpeed: 'V/S',
    touchdownNormalVelocity: 'Landing Rate',
    gForce: 'G-Force',
    fuelTotalQuantity: 'Total Fuel',
    isUnlimitedFuel: 'Unlimited Fuel',
    barometerPressure: 'Pressure',
    totalAirTemperature: 'Air Temp',
    windVelocity: 'Wind',
    windDirection: 'Wind Direction',
    isOnGround: 'On Ground',
    stallWarning: 'Stalling',
    overspeedWarning: 'Overspeed',
    isAutopilotOn: 'Autopilot',
    transponderState: 'Trans. State',
    transponder: 'Trans. Code',
}

const columnsUnselected = {
    clientVersion: 'Version',
    transponder: 'Trans. Code',
}

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

hub.onreconnected(async connectionId => {
    console.log('Reconnected to SignalR with connection ID ' + connectionId);
})

const DataPage = () => {
    const [aircraftInfos, setAircraftInfos] = React.useState<{ [string: string]: AircraftData }>({})
    const [aircraftStatuses, setAircraftStatuses] = React.useState<{ [string: string]: AircraftStatus }>({})
    const [selectedClientIds, setSelectedClientIds] = React.useState<string[]>([])

    React.useEffect(() => {
        (async () => {
            hub.onreconnected(async connectionId => {
                console.log('Connected to SignalR with connection ID ' + connectionId);
                await hub.send('LoginAdmin');
            });
            hub.on('UpdateAircraft', (clientId, aircraftStatus: AircraftStatus) => {
                aircraftStatus = convertPropertyNames(aircraftStatus, pascalCaseToCamelCase) as AircraftStatus;

                setAircraftStatuses(aircrafts => ({
                    ...aircrafts,
                    [clientId]: aircraftStatus
                }));
            });
            hub.on('ReturnAircraftInfo', (clientId, aircraftData: AircraftData) => {
                setAircraftInfos(aircrafts => ({
                    ...aircrafts,
                    [clientId]: aircraftData
                }))
            });

            await hub.start();
            await hub.send('LoginAdmin');

            return () => {
                hub.stop();
            }
        })();
    }, []);

    const clientIds = Object.keys(aircraftStatuses);

    const handleAdd = async (clientId) => {
        setSelectedClientIds(selectedClientIds.concat([clientId]));
        await hub.send('RequestAircraftInfo', clientId);
    }

    const handleRemove = (clientId) => {
        setSelectedClientIds(selectedClientIds.filter(o => o !== clientId));
    }

    return <>
        <h3>Data</h3>

        <div className="table-responsive">
            <table className="table table-bordered">
                <thead>
                    <tr>
                        <th>&nbsp;</th>
                        <th>Callsign</th>
                        <th>Type</th>
                        {Object.keys(columnsSelected).map(key => <th key={key}>{columnsSelected[key]}</th>)}
                    </tr>
                </thead>
                <tbody>
                    {clientIds.map(clientId => {
                        if (!selectedClientIds.includes(clientId))
                            return null;

                        return <AircraftRow key={clientId} clientId={clientId} aircraftData={aircraftInfos[clientId]} aircraft={aircraftStatuses[clientId]} onTick={handleRemove} selected />
                    })}
                </tbody>
            </table>
        </div>

        <table className="table table-bordered">
            <thead>
                <tr>
                    <th>&nbsp;</th>
                    <th>Callsign</th>
                    {Object.keys(columnsUnselected).map(key => <th key={key}>{columnsSelected[key]}</th>)}
                </tr>
            </thead>
            <tbody>
                {clientIds.map(clientId => {
                    if (selectedClientIds.includes(clientId))
                        return null;

                    return <AircraftRow key={clientId} clientId={clientId} aircraftData={aircraftInfos[clientId]} aircraft={aircraftStatuses[clientId]} onTick={handleAdd} />
                })}
            </tbody>
        </table>
    </>
}

const AircraftRow = (props: { selected?: boolean, clientId: string, aircraftData: AircraftData, aircraft: AircraftStatus, onTick: (clientId: string) => void }) => {
    const { selected, clientId, aircraftData, aircraft, onTick } = props;

    const handleClick = async (parameter: string) => {
        if (!navigator.clipboard) {
            // Clipboard API not available
            return;
        }

        //Get the paragraph text
        try {
            //Write it to the clipboard
            await navigator.clipboard.writeText(`${window.location.protocol}//${window.location.host}/Overlay/Data?callsign=${aircraft.callsign}&parameter=${parameter}`);
        } catch (err) {
            console.error('Failed to copy!', err);
        }
    }

    return <tr>
        <th><input type="checkbox" checked={selected} onChange={() => onTick(clientId)} /></th>
        <td><StyledCallsign>{aircraft.callsign}</StyledCallsign></td>
        {selected && <td><StyledType title={`${aircraftData?.title}\n${aircraftData?.model}\n${aircraftData?.type}\n${aircraftData?.estimatedCruiseSpeed}`}>{aircraftData?.title}</StyledType></td>}
        {Object.keys(selected ? columnsSelected : columnsUnselected).map(key =>
            <td key={key}>
                <span title='Click to copy overlay link' onClick={() => handleClick(key)}>
                    {(aircraft[key] === null || aircraft[key] === undefined) ? "" : (typeof (aircraft[key]) === 'number' ? (Math.round(aircraft[key] * 100) / 100).toString() : aircraft[key].toString())}
                </span>
            </td>
        )}
    </tr>
}

const StyledCallsign = styled.span`
white-space: nowrap;
`

const StyledType = styled.span`
white-space: nowrap;
`

export default DataPage;