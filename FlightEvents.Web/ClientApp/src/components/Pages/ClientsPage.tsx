import * as React from 'react';
import * as signalr from '@microsoft/signalr';

const hub = new signalr.HubConnectionBuilder()
    .withUrl('/FlightEventHub?clientType=Web')
    .withAutomaticReconnect()
    .build();

interface ClientInfo {
    clientType: string;
    clientId: string;
    clientVersion: string;
}

const ClientsPage = () => {
    const [clients, setClients] = React.useState<ClientInfo[]>([]);


    React.useEffect(() => {
        const f = async () => {
            await hub.start();
            await hub.send('LoginAdmin');
        }

        f();
    }, [])

    hub.on('UpdateClientList', clients => {
        setClients(clients);
    })

    const localClients = clients.filter(client => client.clientType === 'Client');
    const otherClients = clients.filter(client => client.clientType !== 'Web' && client.clientType !== 'Client');

    return <>
        <h5>SignalR Connections</h5>

        <h6>Web ({clients.filter(client => client.clientType === 'Web').length})</h6>

        <h6>Client ({localClients.length})</h6>
        <ul>
            {localClients.sort((a, b) => a.clientVersion.localeCompare(b.clientVersion)).map(client => <li key={client.clientId}>{client.clientType} {client.clientVersion} {client.clientId}</li>)}
        </ul>

        <h6>Other ({otherClients.length})</h6>
        <ul>
            {otherClients.map(client => <li key={client.clientId}>{client.clientType} {client.clientVersion} {client.clientId}</li>)}
        </ul>
    </>;
}

export default ClientsPage;