import * as React from 'react';
import styled from 'styled-components';
import { Query } from '@apollo/client/react/components';
import { gql } from '@apollo/client';
import { ApolloQueryResult } from '@apollo/client/core';
import { Container, Row, Col, ButtonGroup, Breadcrumb, BreadcrumbItem, Badge } from 'reactstrap';
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

                    const events = [...data.flightEvents]
                    events.sort((a, b) =>
                        a.startDateTime === b.startDateTime ?
                            (
                                a.endDateTime === b.endDateTime ? 0 : (a.startDateTime < b.startDateTime ? 1 : -1)
                            )
                            :
                            (a.startDateTime < b.startDateTime ? 1 : -1)
                    )

                    return <ul>
                        {events.map(flightEvent => (
                            <li key={flightEvent.id}>
                                <StyledTitle>{flightEvent.name} <Badge>{flightEvent.type}</Badge></StyledTitle>
                                <StyledTime>{flightEvent.startDateTime}</StyledTime>
                                <ButtonGroup>
                                    <Link to={`Events/${flightEvent.id}`} className='btn btn-primary btn-sm'>Details</Link>
                                    {flightEvent.type === 'RACE' && <>
                                        <Link to={`Events/${flightEvent.id}/Stopwatch`} className='btn btn-secondary btn-sm'>Stopwatch</Link>
                                        <Link to={`Events/${flightEvent.id}/Leaderboard`} className='btn btn-info btn-sm'>Leaderboard</Link>
                                    </>}
                                </ButtonGroup>
                            </li>
                        ))}
                    </ul>
                }}</Query>
            </Col>
        </Row>
    </Container>
}

const StyledTitle = styled.div`
font-weight: bold;
margin-top: 10px;
`

const StyledTime = styled.div`
font-size: 0.9em;
`