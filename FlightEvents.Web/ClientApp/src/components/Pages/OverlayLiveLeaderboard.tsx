import * as React from 'react';
import styled, { css } from 'styled-components';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { RouteComponentProps } from 'react-router-dom';
import { Query } from '@apollo/client/react/components';
import { ApolloQueryResult } from '@apollo/client/core';
import { gql } from '@apollo/client';
import { FlightEvent, LeaderboardRecord, Stopwatch } from '../../Models';
import StopwatchHub from '../StopwatchHub';

const QUERY = gql`query getEvent($id: Uuid!) {
    flightEvent(id: $id) {
        id
        checkpoints {
            waypoint
            symbol
        }
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

interface LiveLeaderboardRecord {
    playerName: string;
    score: number;
    scoreDisplay?: string;
    timeDiff?: number;
    isReference?: boolean;
    referencePoint?: string;
    isStopwatch?: boolean;
    stoppedDateTime?: string;
    startedDateTime?: string;
    timeSinceStart: number;
}

export default (props: RouteComponentProps<RouteProps>) => {
    const params = new URLSearchParams(props.location.search);
    const callsignsParam = params.get('callsigns');
    const leaderboardParam = params.get('leaderboard');

    if (!leaderboardParam) {
        return 'Missing leaderboard';
    }

    const callsigns = callsignsParam ? callsignsParam.split(',') : null;

    const [stopwatches, setStopwatches] = React.useState<{ [id: string]: Stopwatch }>({});
    const [leaderboards, setLeaderboards] = React.useState<LeaderboardRecord[]>([]);

    return <Query query={QUERY} variables={{ id: props.match.params.id }}>{({ loading, error, data }: ApolloQueryResult<{ flightEvent: FlightEvent }>) => {
        if (loading) return <>Loading...</>
        if (error) return <>Cannot load event!</>

        const handleUpdateStopwatch = (stopwatch: Stopwatch, serverDateString: string) => {
            window['shift'] = new Date().getTime() - new Date(serverDateString).getTime();

            if (stopwatch.leaderboardName === leaderboardParam && (!callsigns || callsigns.includes(stopwatch.name))) {
                setStopwatches(stopwatches => ({ ...stopwatches, [stopwatch.id]: stopwatch }))
            }
        }

        const handleRemoveStopwatch = (stopwatch: Stopwatch) => {
            setStopwatches(stopwatches => {
                delete stopwatches[stopwatch.id];
                return { ...stopwatches }
            })
        }

        const onUpdateLeaderboard = (records: LeaderboardRecord[]) => {
            setLeaderboards(records.filter(r => r.leaderboardName === leaderboardParam && (!callsigns || callsigns.includes(r.playerName))))
        }

        const result: LiveLeaderboardRecord[] = leaderboards.filter(o => o.subIndex === 0);
        result.sort((a, b) => a.score === b.score ? 0 : (a.score > b.score ? -1 : 1));

        const finishSymbol = '🏁';
        const references = !!data.flightEvent.checkpoints ? data.flightEvent.checkpoints.map(checkpoint => checkpoint.symbol).filter(symbol => !!symbol) : [];
        references.push(finishSymbol);

        if (result.length > 0) {
            result[0].timeDiff = 0;
            result[0].isReference = true;
            result[0].referencePoint = finishSymbol;
            for (let i = 1; i < result.length; i++) {
                result[i].timeDiff = -(result[i].score - result[0].score);
                result[i].referencePoint = finishSymbol;
            }
        }

        let firstRecords = result.length > 0 ? leaderboards.filter(o => o.playerName === result[0].playerName) : null;

        if (!firstRecords) {
            // No one completed the race yet
            // We use the one with the most checkpoint and shorted laptime as reference
            let bestId: string | null = null;
            let bestLapCount = 0;
            let bestTime = Number.MAX_VALUE;
            for (let id of Object.keys(stopwatches)) {
                const stopwatch = stopwatches[id];
                if (stopwatch.startedDateTime) {
                    const time = new Date(stopwatch.lapsDateTime[stopwatch.lapsDateTime.length - 1]).getTime() - new Date(stopwatch.startedDateTime).getTime();
                    if (stopwatch.lapsDateTime.length >= bestLapCount || (stopwatch.lapsDateTime.length === bestLapCount && time < bestTime)) {
                        bestId = id;
                        bestLapCount = stopwatch.lapsDateTime.length;
                        bestTime = time;
                    }
                }
            }

            if (bestId) {
                const stopwatch = stopwatches[bestId];
                firstRecords = [];
                if (stopwatch.startedDateTime) {
                    for (let i = 0; i < stopwatch.lapsDateTime.length; i++) {
                        const time = new Date(stopwatch.lapsDateTime[i]).getTime();
                        firstRecords.push({
                            eventId: data.flightEvent.id,
                            leaderboardName: leaderboardParam,
                            subIndex: i + 1,
                            playerName: stopwatch.name,
                            timeSinceStart: time - new Date(stopwatch.startedDateTime).getTime(),
                            score: 0,
                            scoreDisplay: ''
                        })
                    }
                }
            }
        }

        for (let id of Object.keys(stopwatches)) {
            const stopwatch = stopwatches[id];

            if (stopwatch.startedDateTime) {
                if (firstRecords && stopwatch.lapsDateTime.length > 0) {
                    const lastLapTime = Math.round(new Date(stopwatch.lapsDateTime[stopwatch.lapsDateTime.length - 1]).getTime() - new Date(stopwatch.startedDateTime).getTime());

                    const referenceTime = firstRecords.find(o => o.subIndex === stopwatch.lapsDateTime.length);
                    result.push({
                        ...stopwatch,
                        playerName: stopwatch.name,
                        score: -lastLapTime,
                        timeSinceStart: lastLapTime,
                        isStopwatch: true,
                        timeDiff: referenceTime ? (lastLapTime - referenceTime.timeSinceStart) : undefined,
                        isReference: firstRecords[0].playerName === stopwatch.name,
                        referencePoint: firstRecords[0].playerName !== stopwatch.name ? references[stopwatch.lapsDateTime.length - 1] : ''
                    })
                } else {
                    result.push({
                        ...stopwatch,
                        playerName: stopwatch.name,
                        score: 0,
                        timeSinceStart: 0,
                        isStopwatch: true
                    })
                }
            }
        }

        result.sort((a, b) => {
            if (a.timeDiff === undefined && b.timeDiff === undefined) {
                return a.playerName < b.playerName ? -1 : 1;
            } else if (a.timeDiff !== undefined && b.timeDiff === undefined) {
                return -1;
            } else if (a.timeDiff === undefined && b.timeDiff !== undefined) {
                return 1;
            } else if (a.timeDiff !== undefined && b.timeDiff !== undefined) {
                return a.timeDiff > b.timeDiff ? 1 : -1;
            }
            return 0;
        });

        return <>
            <StopwatchHub
                eventId={data.flightEvent.id}
                hub={hub}
                onUpdateStopwatch={handleUpdateStopwatch}
                onRemoveStopwatch={handleRemoveStopwatch}
                onUpdateLeaderboard={onUpdateLeaderboard}
            />

            <StyledTable>
                <tbody>
                    {result.map((l, index) => (
                        <tr key={l.playerName}>
                            <StyledRank highlight={l.isReference}>
                                <div>{l.timeDiff === undefined ? '' : (index + 1)}</div>
                            </StyledRank>
                            <StyledName highlight={l.isReference}>
                                {l.playerName}
                            </StyledName>
                            <StyledRef highlight={l.isReference}>
                                {l.referencePoint}
                            </StyledRef>
                            <StyledDiff highlight={l.isReference}>
                                {l.timeDiff !== undefined && formatTime(l.timeDiff, true)}
                            </StyledDiff>
                            <StyledTime highlight={l.isReference}>
                                {l.isReference && !l.isStopwatch ? formatTime(l.timeSinceStart, false) : (!!l.isStopwatch && <StopwatchItem {...l} />)}
                            </StyledTime>
                        </tr>
                    ))}
                </tbody>
            </StyledTable>
        </>
    }}
    </Query>
}

interface StopwatchItemProps {
    stoppedDateTime?: string;
    startedDateTime?: string;
}

const StopwatchItem = (props: StopwatchItemProps) => {
    const [elapsed, setElapsed] = React.useState<number>(0);

    React.useEffect(() => {
        if (!props.stoppedDateTime && props.startedDateTime) {
            const start = new Date(props.startedDateTime).getTime();
            const interval = setInterval(() => {
                setElapsed(new Date().getTime() - start - window['shift'])
            }, 100);

            return () => {
                clearInterval(interval)
            }
        } else if (props.stoppedDateTime && props.startedDateTime) {
            setElapsed(new Date(props.stoppedDateTime).getTime() - new Date(props.startedDateTime).getTime())
        }
    }, [props])

    return <>{formatTime(elapsed, false)}</>
}


function formatTime(elapsed: number, hasSign: boolean) {
    if (elapsed === 0) return '-';
    let result = hasSign ? '+' : '';
    if (elapsed < 0) {
        result = '-';
        elapsed = -elapsed;
    }
    const hour = Math.floor(elapsed / 1000 / 3600) % 60;
    const minute = Math.floor(elapsed / 1000 / 60) % 60;
    const second = Math.floor(elapsed / 1000) % 60;
    if (hour > 0) {
        result += `${hour.toString()}:${minute.toString().padStart(2, '0')}:${(Math.floor(elapsed / 1000) % 60).toString().padStart(2, '0')}.${Math.floor(elapsed % 1000 / 10).toString().padStart(2, '0')}`;
    } else if (minute > 0) {
        result += `${minute.toString()}:${second.toString().padStart(2, '0')}.${Math.floor(elapsed % 1000 / 10).toString().padStart(2, '0')}`;
    } else {
        result += `${second.toString()}.${Math.floor(elapsed % 1000 / 10).toString().padStart(2, '0')}`;
    }
    return result;
}

const StyledTable = styled.table`
color: white;
`

interface CellProps {
    highlight: boolean;
}

const StyledRank = styled.td<CellProps>`
${props => props.highlight ? css`background: rgba(17, 99, 65, 0.8);` : css`background: rgba(0, 0, 0, 0.8);`}
font-weight: bold;
padding: 4px;

div {
    width: 24px;
    height: 24px;
    background: rgba(255, 255, 255, 0.8);
    color: black;
    text-align: center;
    border-radius: 4px;
}
`

const StyledName = styled.td<CellProps>`
${props => props.highlight ? css`background: rgba(17, 99, 65, 0.8);` : css`background: rgba(0, 0, 0, 0.8);`}
font-weight: bold;
padding: 4px 14px 4px 4px;
`

const StyledRef = styled.td<CellProps>`
${props => props.highlight ? css`background: rgba(17, 99, 65, 0.5);` : css`background: rgba(0, 0, 0, 0.5);`}
font-weight: 600;
padding: 4px 6px 4px 6px;
`

const StyledDiff = styled.td<CellProps>`
${props => props.highlight ? css`background: rgba(17, 99, 65, 0.5);` : css`background: rgba(0, 0, 0, 0.5);`}
font-weight: 600;
padding: 4px 6px 4px 14px;
width: 100px;
text-align: right;
`

const StyledTime = styled.td<CellProps>`
${props => props.highlight ? css`background: rgba(17, 99, 65, 0.5);` : css`background: rgba(0, 0, 0, 0.5);`}
font-weight: 600;
padding: 4px 14px 4px 6px;
width: 100px;
`