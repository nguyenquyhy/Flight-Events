import * as React from 'react';
import styled from 'styled-components';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { RouteComponentProps } from 'react-router-dom';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { FlightEvent, LeaderboardRecord, Stopwatch } from '../../Models';
import StopwatchHub from '../StopwatchHub';

const QUERY = gql`query getEvent($id: UUID!) {
    flightEvent(id: $id) {
        id
    }
}`

interface RouteProps {
    id: string;
}

const hub = new HubConnectionBuilder()
    .withUrl('/FlightEventHub')
    .withAutomaticReconnect()
    .build();

window['shift'] = 0;

export default (props: RouteComponentProps<RouteProps>) => {
    const params = new URLSearchParams(props.location.search);
    const callsign = params.get('callsign');
    const leaderboardParam = params.get('leaderboard');
    const leaderboardIndex = Number(params.get('index') ?? '0');

    if (!callsign) {
        return <p>Missing callsign</p>;
    }

    const [stopwatch, setStopwatch] = React.useState<Stopwatch | null>(null);
    const [leaderboard, setLeaderboard] = React.useState<LeaderboardRecord | null>(null);

    return <Query query={QUERY} variables={{ id: props.match.params.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
        if (loading) return <>Loading...</>
        if (error) return <>Cannot load event!</>

        const event = data.flightEvent;

        const handleUpdateStopwatch = (updatedStopwatch: Stopwatch, serverDateString) => {
            window['shift'] = new Date().getTime() - new Date(serverDateString).getTime();

            if (callsign === updatedStopwatch.name && (!leaderboardParam || leaderboardParam === updatedStopwatch.leaderboardName)) {
                setStopwatch(updatedStopwatch)
            }
        }

        const handleRemoveStopwatch = (removedStopwatch: Stopwatch) => {
            if (!!stopwatch && stopwatch.id === removedStopwatch.id) {
                setStopwatch(null)
            }
        }

        const onUpdateLeaderboard = (records: LeaderboardRecord[]) => {
            var record = records.find(o =>
                callsign === o.playerName &&
                (!leaderboardParam || leaderboardParam === o.leaderboardName) &&
                o.subIndex === leaderboardIndex
            ) ?? null;
            setLeaderboard(record);
        }

        const content = !!leaderboard ?
            <StyledTime>{leaderboard.scoreDisplay}</StyledTime>
            :
            (!!stopwatch ?
                (
                    leaderboardIndex > 0 && stopwatch.lapsDateTime.length + 1 >= leaderboardIndex && stopwatch.startedDateTime ?
                        (stopwatch.lapsDateTime.length + 1 > leaderboardIndex ?
                            <StyledTime>
                                {formatTime(
                                    new Date(stopwatch.lapsDateTime[leaderboardIndex - 1]).getTime() -
                                    new Date(leaderboardIndex > 1 ? stopwatch.lapsDateTime[leaderboardIndex - 2] : stopwatch.startedDateTime).getTime())}
                            </StyledTime> :
                            <StopwatchItem lapIndex={leaderboardIndex} {...stopwatch} />
                        )
                        :
                        leaderboardIndex === 0 && stopwatch.name === callsign && (!leaderboardParam || stopwatch.leaderboardName === leaderboardParam) ?
                            <StopwatchItem {...stopwatch} />
                            :
                            null
                )
                :
                null
            );

        return <>
            <StopwatchHub
                eventId={event.id}
                hub={hub}
                onUpdateStopwatch={handleUpdateStopwatch}
                onRemoveStopwatch={handleRemoveStopwatch}
                onUpdateLeaderboard={onUpdateLeaderboard}
            />
            {content}
        </>
    }}
    </Query>
}

interface ItemProps {
    lapIndex?: number;
}

interface ItemState {
    elapsed: number;
}

function formatTime(elapsed: number) {
    return `${(Math.floor(elapsed / 1000 / 3600) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000 / 60) % 60).toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000) % 60).toString().padStart(2, '0')}.${(elapsed % 1000).toString().padStart(3, '0')}`;
}

const StopwatchItem = (props: ItemProps & Stopwatch) => {
    const [state, setState] = React.useState<ItemState>({ elapsed: 0 });

    React.useEffect(() => {
        if (!props.stoppedDateTime && props.startedDateTime) {
            const start = new Date(!!props.lapIndex && props.lapIndex > 1 ? props.lapsDateTime[props.lapIndex - 2] : props.startedDateTime).getTime();
            const interval = setInterval(() => {
                setState({ elapsed: new Date().getTime() - start - window['shift'] })
            }, 100);

            return () => {
                clearInterval(interval)
            }
        } else if (props.stoppedDateTime && props.startedDateTime) {
            setState({ elapsed: new Date(props.stoppedDateTime).getTime() - new Date(props.startedDateTime).getTime() })
        }
    }, [props])

    return <StyledTime>{formatTime(state.elapsed)}</StyledTime>
}

const StyledTime = styled.span`
font-size: 2em;
font-family: Courier New,Courier,Lucida Sans Typewriter,Lucida Typewriter,monospace;
position: relative;
`