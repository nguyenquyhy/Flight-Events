import * as React from 'react';
import styled from 'styled-components';
import { Container, Button } from 'reactstrap';
import { FlightPlanCompact } from '../Models';

interface State {
    flightPlans: FlightPlanCompact[] | null;
}

export default () => {
    var [state, setState] = React.useState<State>({ flightPlans: null })
    React.useEffect(() => {
        const f = async () => {
            var response = await fetch('/api/FlightPlans')
            var data = await response.json() as FlightPlanCompact[]
            setState({ flightPlans: data })
        }
        f()
    }, [])

    if (state.flightPlans === null) {
        return <Container>
            <h3>Flight Plans</h3>

            <p>Loading...</p>
        </Container>
    }

    const handleDelete = async (callsign) => {
        if (window.confirm(`Delete flight plan '${callsign}'?`)) {
            await fetch(`/api/FlightPlans/${callsign}`, {
                method: 'DELETE'
            })
            window.location.reload();
        }
    }

    return <Container>
        <h3>Flight Plans</h3>

        <ul>
            {state.flightPlans.map(fp => (
                <li key={fp.callsign}>
                    <h6>{fp.callsign}</h6>
                    <Line>{fp.departure} - {fp.destination} {fp.alternate ? ("(" + fp.alternate + ")") : ""}</Line>
                    <Line>Cruising at {fp.cruisingAltitude}ft at {fp.cruisingSpeed}kt</Line>
                    <Line>{fp.route ? ("Route: " + fp.route) : ""}</Line>
                    <Line>{fp.remarks ? ("Remarks: " + fp.remarks) : ""}</Line>
                    <Button color="danger" onClick={() => handleDelete(fp.callsign)}>Delete</Button>
                </li>
            ))}
        </ul>
    </Container>
}

const Line = styled.div`
display: block;
`