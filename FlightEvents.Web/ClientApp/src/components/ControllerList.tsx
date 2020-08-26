import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody } from 'reactstrap';
import { ATCStatus, ATCInfo } from '../Models';

interface Props {
    controllers: { [clientId: string]: ATCInfo & ATCStatus };
    onControllerClick: (clientId: string) => void;
}

interface State {
    collapsed: boolean;
}

export default (props: Props) => {
    const [state, setState] = React.useState<State>({ collapsed: false });
    const handleToggle = () => {
        setState({
            collapsed: !state.collapsed
        })
    };

    let clientIds = Object
        .entries(props.controllers)
        .sort((a, b) => (a[1].callsign || a[0].substring(5)).localeCompare((b[1].callsign || b[0].substring(5))))
        .map(o => o[0]);

    if (clientIds.length === 0) return null;

    const list = clientIds.length === 0 ?
        <tr><td colSpan={1}>None</td></tr> :
        clientIds.map(clientId => (
            <tr key={clientId}>
                <td>
                    <button className="btn btn-link" onClick={(function () { props.onControllerClick(clientId); })}>{props.controllers[clientId].callsign || clientId.substring(5)}</button>
                </td>
            </tr>));

    return <>
        <StyledButton color="secondary" onClick={handleToggle}>Controllers ({clientIds.length})</StyledButton>
        <Modal isOpen={state.collapsed} toggle={handleToggle}>
            <ModalHeader>
                Controller List {(clientIds.length === 0 ? "" : `(${clientIds.length})`)}
            </ModalHeader>
            <ModalBody>
                <table>
                    <thead>
                        <tr>
                            <th><div>Callsign</div></th>
                        </tr>
                    </thead>
                    <tbody>
                        {list}
                    </tbody>
                </table>
            </ModalBody>
        </Modal>
    </>
}

const StyledButton = styled(Button)`
position: fixed;
top: 10px;
right: 290px;
width: 130px;
z-index: 1000;
`