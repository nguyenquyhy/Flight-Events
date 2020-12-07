import * as React from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Container, Form, FormGroup, Label, Input, Button } from 'reactstrap'

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

interface State {
    callsign: string;
    type: string;
    departure: string;
    destination: string;
    alternate: string;
    cruisingAltitude: number;
    cruisingSpeed: number;
    route: string;
    remarks: string;
}

export default () => {
    let [pageState, setPageState] = React.useState<string>('IDLE');
    let [state, setState] = React.useState<State>({
        callsign: '',
        type: '',
        departure: '',
        destination: '',
        alternate: '',
        cruisingAltitude: 10000,
        cruisingSpeed: 150,
        route: '',
        remarks: ''
    });


    React.useEffect(() => {
        const f = async () => {
            setPageState('CONNECTING');
            await hub.start();
            setPageState('CONNECTED');
        }
        f();

        return () => {
            hub.stop();
        }
    }, [])

    if (pageState === 'IDLE' || pageState === 'CONNECTING') {
        return <Container>
            <h3>Flight Plan</h3>
            <p>Connecting to server...</p>
        </Container>
    }

    if (pageState === 'SUBMITTING') {
        return <Container>
            <h3>Flight Plan</h3>
            <p>Sending flight plan...</p>
        </Container>
    }

    if (pageState === 'SUBMITTED') {
        return <Container>
            <h3>Flight Plan</h3>
            <p>Your flight plan is submitted.</p>
        </Container>
    }

    const handleCallsignChanged = (e) => setState({ ...state, callsign: e.target.value });
    const handleTypeChanged = (e) => setState({ ...state, type: e.target.value });
    const handleDepartureChanged = (e) => setState({ ...state, departure: e.target.value });
    const handleDestinationChanged = (e) => setState({ ...state, destination: e.target.value });
    const handleAlternateChanged = (e) => setState({ ...state, alternate: e.target.value });
    const handleCruisingAltitudeChanged = (e) => setState({ ...state, cruisingAltitude: Number(e.target.value) });
    const handleCruisingSpeedChanged = (e) => setState({ ...state, cruisingSpeed: Number(e.target.value) });
    const handleRouteChanged = (e) => setState({ ...state, route: e.target.value });
    const handleRemarksChanged = (e) => setState({ ...state, remarks: e.target.value });

    const handleSubmit = async (e) => {
        e.preventDefault();

        setPageState('SUBMITTING');
        await hub.send('addFlightPlan', null, state.callsign, 'Web', {
            callsign: state.callsign,
            type: state.type,
            departure: state.departure,
            destination: state.destination,
            cruisingAltitude: state.cruisingAltitude,
            cruisingSpeed: state.cruisingSpeed,
            route: state.route,
            remarks: state.remarks
        })
        setPageState('SUBMITTED');
    }

    return <Container>
        <Form onSubmit={handleSubmit}>
            <h3>Flight Plan</h3>
            <FormGroup>
                <Label htmlFor="callsign">Callsign</Label>
                <Input name="callsign" className="form-control" required value={state.callsign} onChange={handleCallsignChanged} />
            </FormGroup>

            <FormGroup tag="fieldset">
                <FormGroup check><Label check><Input name="type" type="radio" value="IFR" onChange={handleTypeChanged} /> IFR</Label></FormGroup>
                <FormGroup check><Label check><Input name="type" type="radio" value="VFR" onChange={handleTypeChanged} /> VFR</Label></FormGroup>
            </FormGroup>

            <FormGroup>
                <Label htmlFor="departure">Departure</Label>
                <Input name="departure" className="form-control" required value={state.departure} onChange={handleDepartureChanged} />
            </FormGroup>
            <FormGroup>
                <Label htmlFor="destination">Destination</Label>
                <Input name="destination" className="form-control" required value={state.destination} onChange={handleDestinationChanged} />
            </FormGroup>
            <FormGroup>
                <Label htmlFor="alternate">Alternate</Label>
                <Input name="alternate" className="form-control" value={state.alternate} onChange={handleAlternateChanged} />
            </FormGroup>

            <FormGroup>
                <Label htmlFor="cruisingAltitude">Cruising Altitude (ft)</Label>
                <Input name="cruisingAltitude" className="form-control" type="number" value={state.cruisingAltitude} onChange={handleCruisingAltitudeChanged} />
            </FormGroup>
            <FormGroup>
                <Label htmlFor="cruisingSpeed">Cruising Speed (kt)</Label>
                <Input name="cruisingSpeed" className="form-control" type="number" value={state.cruisingSpeed} onChange={handleCruisingSpeedChanged} />
            </FormGroup>

            <FormGroup>
                <Label htmlFor="route">Route</Label>
                <Input name="route" type="textarea" className="form-control" value={state.route} onChange={handleRouteChanged} />
            </FormGroup>

            <FormGroup>
                <Label htmlFor="remarks">Remarks</Label>
                <Input name="remarks" type="textarea" className="form-control" value={state.remarks} onChange={handleRemarksChanged} />
            </FormGroup>

            <Button type="submit">Submit</Button>
        </Form>
    </Container>
}