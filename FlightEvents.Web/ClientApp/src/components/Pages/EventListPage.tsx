import * as React from 'react';
import { Query } from '@apollo/client/react/components';
import { gql } from '@apollo/client';
import { ApolloQueryResult } from '@apollo/client/core';
import { Container, Row, Col, ButtonGroup, Breadcrumb, BreadcrumbItem } from 'reactstrap';
import { Link } from 'react-router-dom';
import { FlightEvent } from '../../Models';

const QUERY = gql`
query {
    flightEvents {
        id
        name
        type
        startDateTime
    }
}
`

export default () => {
    return <Container>
        <Row>
            <Col>
                <Breadcrumb>
                    <BreadcrumbItem><Link to='/'>🗺</Link></BreadcrumbItem>
                    <BreadcrumbItem>Events</BreadcrumbItem>
                </Breadcrumb>
                </Col>
            </Row>
        <Row>
            <Col>
                <Query query={QUERY}>{({ loading, error, data }: ApolloQueryResult<{ flightEvents: FlightEvent[] }>) => {
                    if (loading) return <p>Loading...</p>
                    if (error) return <p>Cannot load events!</p>

                    return <ul>
                        {data.flightEvents.map(flightEvent => (
                            <li key={flightEvent.id}>
                                <h6>{flightEvent.name}</h6>
                                <p>{flightEvent.startDateTime}</p>
                                <p>{flightEvent.type}</p>
                                <ButtonGroup>
                                    <Link to={`Events/${flightEvent.id}`} className='btn btn-primary'>Details</Link>
                                    <Link to={`Events/${flightEvent.id}/Stopwatch`} className='btn btn-secondary'>Stopwatch</Link>
                                </ButtonGroup>
                            </li>
                        ))}
                    </ul>
                }}</Query>
            </Col>
        </Row>
    </Container>
}