import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, ModalFooter } from 'reactstrap';
import { FlightEvent } from '../Models';
import Api from '../Api';
import parseISO from 'date-fns/parseISO';
import ReactMarkdown from 'react-markdown';

interface Props {
    flightEvent: FlightEvent;
    isOpen: boolean;
    toggle: () => void;
}

interface State {
    isLoading: boolean;
    flightEvent: FlightEvent | null;
}

export default class EventModal extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);

        this.state = {
            isLoading: true,
            flightEvent: null
        }

        this.handleOpen = this.handleOpen.bind(this);
    }

    private async handleOpen() {
        if (!this.state.flightEvent) {
            const event = await Api.getFlightEvent(this.props.flightEvent.id);
            this.setState({
                isLoading: false,
                flightEvent: event
            });
        }
    }

    public render() {
        const details = this.state.flightEvent ?
            <>
                <div><StyledTime>{parseISO(this.state.flightEvent.startDateTime).toLocaleString()}</StyledTime></div>
                <div><ReactMarkdown>{this.state.flightEvent.description}</ReactMarkdown></div>
            </> :
            <div>Loading...</div>;


        return <Modal isOpen={this.props.isOpen} toggle={this.props.toggle} onOpened={this.handleOpen} size='lg'>
            <ModalHeader>{this.props.flightEvent.name}</ModalHeader>
            <ModalBody>
                {details}
            </ModalBody>
            <ModalFooter>
                <Button color="primary" disabled>Join</Button>{' '}
                <Button color="secondary">Close</Button>
            </ModalFooter>
        </Modal>
    }
}

const StyledTime = styled.span`
border-bottom: 1px dashed #909090;
margin-bottom: 10px;
`