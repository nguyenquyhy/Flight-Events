import * as React from 'react';
import { Modal, ModalBody, ModalHeader } from 'reactstrap';
import { MapPosition } from '../../maps/IMap';

interface Props {
    onRequested: (code: string, position: MapPosition, altitude: number) => void;
    onComplete: () => void;
    selectedPosition: MapPosition | null;
}

interface State {
    altitude: number;
    teleportToken: string | null;
}

const TeleportDialog = (props: Props) => {
    let [state, setState] = React.useState<State>({ altitude: 10000, teleportToken: null });

    const send = (position: MapPosition, altitude: number) => {
        let code = '';
        for (let i = 0; i < 6; i++) {
            code += String.fromCharCode(Math.floor(Math.random() * 26) + 65);
        }
        props.onRequested(code, position, altitude);

        return code;
    }

    return (
        <Modal isOpen={!!props.selectedPosition} toggle={() => { setState({ ...state, teleportToken: null }); props.onComplete(); }}>
            <ModalHeader>
                Teleport Aircraft
                </ModalHeader>
            <ModalBody>
                <table className="table table-sm">
                    <thead>
                        <tr><th colSpan={2}>Target</th></tr>
                    </thead>
                    <tbody>
                        <tr><td>Latitude</td><td>{props.selectedPosition && props.selectedPosition.latitude}</td></tr>
                        <tr><td>Longitude</td><td>{props.selectedPosition && props.selectedPosition.longitude}</td></tr>
                        {!!state.teleportToken && <tr><td>Altitude</td><td>{state.altitude}ft</td></tr>}
                    </tbody>
                </table>
                {!state.teleportToken && <>
                    <div>Please enter the required altitude (ft):</div>
                    <input type="number" className="form-control" value={state.altitude} onChange={e => setState({ ...state, altitude: parseFloat(e.target.value) })} />
                    <button className="btn btn-primary" onClick={() => props.selectedPosition && setState({ ...state, teleportToken: send(props.selectedPosition, state.altitude) })}>Continue</button>
                </>}

                {state.teleportToken && <>
                    <div>Please enter the following token to your Flight Events client.</div>
                    <div className="alert alert-primary" role="alert" style={{ textAlign: "center" }}>
                        <strong>{state.teleportToken}</strong>
                    </div>
                    
                </>}
            </ModalBody>
        </Modal>
    );
}

export default React.memo(TeleportDialog);