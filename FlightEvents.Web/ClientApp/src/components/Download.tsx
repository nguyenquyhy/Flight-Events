import * as React from 'react';
import { Button, Modal, ModalHeader, ModalBody } from 'reactstrap';

interface State {
    modal: boolean;
}

export default class Download extends React.Component<any, State> {
    constructor(props: any) {
        super(props);

        this.state = {
            modal: false
        }

        this.toggle = this.toggle.bind(this);
    }

    toggle() {
        this.setState({
            modal: !this.state.modal
        });
    }

    public render() {
        return <>
            <Button color="primary" onClick={this.toggle} style={{ minWidth: '150px' }}>Join</Button>
            <Modal isOpen={this.state.modal} toggle={this.toggle}>
                <ModalHeader toggle={this.toggle}>Flight Events Client</ModalHeader>
                <ModalBody>
                    <p>In order to connect your flight simulator to this map, you have to run a small client software on your computer.</p>

                    <a className="btn btn-primary" href="https://events-storage.flighttracker.tech/downloads/FlightEvents.Client.zip" target="_blank" rel="noopener noreferrer">Download Client</a>

                    <hr />
                    <p>When you start the client, it might also ask you to download &amp; install the following prerequisites:</p>
                    <ul>
                        <li><a href="https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.5-windows-x86-installer" target="_blank" rel="noopener noreferrer">.NET Core Windows Runtime (x86)</a></li>
                    </ul>

                    <hr />
                    <h5>Flight Tracking</h5>
                    <p>Type in your preferred callsign and click Start Flight Tracking to show your aircraft on the map.</p>
                    <h5>Discord Connection</h5>
                    <p>The client can automatically switch you among frequency channels in participating Discord server.<br />You can enable this by connecting your Discord account, starting flight tracking and then connecting to the designated lounge Voice Channel in the Discord server.</p>
                    <h5>ATC</h5>
                    <p>The client can act as a FSD server for compatible ATC clients such as Euroscope and Aurora.<br/>You just need to click Start ATC mode and connect your favorite ATC client to localhost.</p>
                </ModalBody>
            </Modal>
        </>
    }
}