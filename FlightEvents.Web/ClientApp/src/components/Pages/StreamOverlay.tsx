import * as React from 'react';
import * as signalr from '@microsoft/signalr';
import { RouteComponentProps } from 'react-router-dom';
import styled, { css } from 'styled-components';

interface Racer {
    eventId: string;
    callsign: string;
    latitude?: number;
    longitude?: number;
    checkpointTimes: number[];
}

interface RacerDisplay {
    rank: number;
    callsign: string;
    lapDiff?: number;
    rankChange?: number;
}

//const dummy: RacerDisplay[] = [
//    {
//        rank: 1,
//        callsign: 'Simtom',
//        lapDiff: 0,
//        rankChange: 0
//    },
//    {
//        rank: 2,
//        callsign: 'AAL1206',
//        lapDiff: 0.212,
//        rankChange: -1
//    },
//    {
//        rank: 3,
//        callsign: 'AC-DXN',
//        lapDiff: 0.532,
//        rankChange: 1
//    },
//    {
//        rank: 4,
//        callsign: 'LT-MEV',
//        lapDiff: 0.854,
//        rankChange: -2
//    },
//    {
//        rank: 5,
//        callsign: 'N930TB',
//        lapDiff: 1.123,
//        rankChange: 1
//    },
//    {
//        rank: 6,
//        callsign: 'Bulletman',
//        lapDiff: 1.436,
//        rankChange: 1
//    },
//    {
//        rank: 7,
//        callsign: 'OddSobriquet',
//        lapDiff: 1.923,
//        rankChange: 0
//    },
//    {
//        rank: 8,
//        callsign: 'PH-MWE',
//        lapDiff: 3.672,
//        rankChange: -1
//    },
//    {
//        rank: 9,
//        callsign: 'STB747400',
//        lapDiff: 4.230,
//        rankChange: 1
//    },
//    {
//        rank: 10,
//        callsign: 'T-TCR',
//        lapDiff: 10.232,
//        rankChange: 0
//    }
//]

const hub = new signalr.HubConnectionBuilder()
    .withUrl('/FlightEventHub?clientType=StreamOverlay')
    .withAutomaticReconnect()
    //.withHubProtocol(new protocol.MessagePackHubProtocol())
    .build();

function compare(lastCheckpoint: number) {
    return function (a: Racer, b: Racer) {
        const aLength = Math.min(a.checkpointTimes.length, lastCheckpoint);
        const bLength = Math.min(b.checkpointTimes.length, lastCheckpoint);
        if (aLength === bLength) {
            const aTime = a.checkpointTimes[aLength - 1];
            const bTime = b.checkpointTimes[bLength - 1];
            return aTime < bTime ? -1 : aTime > bTime ? 1 : 0;
        } else {
            return aLength > bLength ? -1 : 1;
        }
    }
}

interface State {
    currentCheckpoint: number;
    racers: RacerDisplay[];
}

interface RouteProps {
    id: string;
}

export default (props: RouteComponentProps<RouteProps>) => {
    var [state, setState] = React.useState<State>({ currentCheckpoint: 0, racers: [] });

    hub.on('UpdateRaceResult', (racers: Racer[]) => {
        if (racers.length === 0) {
            setState({ currentCheckpoint: 0, racers: [] });
            return;
        }

        const result: RacerDisplay[] = [];

        const checkpoints = racers.map(racer => racer.checkpointTimes.length);
        const lastCheckpoint = checkpoints.reduce((prev, curr) => Math.max(prev, curr), 0);

        racers = racers.sort(compare(lastCheckpoint));

        const firstCheckpointTime = racers[0].checkpointTimes[lastCheckpoint - 1];
        for (let i = 0; i < racers.length; i++) {
            const racer = racers[i];
            result.push({
                rank: i + 1,
                callsign: racer.callsign,
                lapDiff: racer.checkpointTimes.length < lastCheckpoint ?
                    undefined : (racer.checkpointTimes[lastCheckpoint - 1] - firstCheckpointTime)
            });
        }

        // Calculate rank change
        const lastRank = racers.sort(compare(lastCheckpoint - 1)).reduce((prev, racer, index) => { prev[racer.callsign] = index + 1; return prev; }, {} as { [callsign: string]: number });
        for (let i = 0; i < result.length; i++) {
            const item = result[i];
            if (item.lapDiff !== undefined) {
                item.rankChange = item.rank - lastRank[item.callsign];
            }
        }

        setState({
            currentCheckpoint: lastCheckpoint,
            racers: result
        })
    });

    React.useEffect(() => {
        (async () => {
            hub.onreconnected(async connectionId => {
                console.log('Connected to SignalR with connection ID ' + connectionId);
                await hub.send('Join', 'StreamOverlay:' + props.match.params.id);
            });

            await hub.start();

            await hub.send('Join', 'StreamOverlay:' + props.match.params.id);
        })();
        return () => {
            (async () => await hub.stop())();
        };
    }, [props.match.params.id]);

    return <>
        <Wrapper>
            <WrapperTitle>{state.racers.length ? "Checkpoint " + state.currentCheckpoint : "Not started"}</WrapperTitle>
            {state.racers.map(item => (
                <Row>
                    <Rank><div>{item.rank}</div></Rank>
                    <Name>{item.callsign}</Name>
                    <LapDiff>{item.lapDiff === undefined ? "\u00A0" : item.lapDiff > 0 ? ("+" + item.lapDiff) : item.lapDiff < 0 ? item.lapDiff : "-"}</LapDiff>
                    <RankChange type={item.rankChange === undefined ? "" : item.rankChange < 0 ? "UP" : item.rankChange > 0 ? "DOWN" : "-"}>{item.rankChange === undefined ? "\u00A0" : item.rankChange < 0 ? "ᴧ" : item.rankChange > 0 ? "ᴠ" : ""} {item.rankChange === undefined ? "\u00A0" : item.rankChange !== 0 ? Math.abs(item.rankChange) : "-"}</RankChange>
                </Row>
            ))}
        </Wrapper>
    </>
}

const Wrapper = styled.div`
display: block;
color: white;
font-weight: bold;
`

const WrapperTitle = styled.div`
display:block;
background-color: rgba(0, 0, 0);
width: 150px;
padding: 5px 10px;
border-radius: 4px 4px 0 0;
`

const Row = styled.div`

`

const Rank = styled.div`
display: inline-block;
background-color: rgba(0, 0, 0, 0.8);
padding: 5px;

div {
    color:black;
    background: white;
    width: 24px;
    height: 24px;
    border-radius: 4px;
    text-align: center;
}
`

const Name = styled.div`
display: inline-block;
background-color: rgba(0, 0, 0, 0.8);
padding: 5px;
width: 200px;
text-transform: uppercase;
`

const LapDiff = styled.div`
display: inline-block;
background-color: rgba(0, 0, 0, 0.5);
padding: 5px;
width: 80px;
`

interface RankChangeProps {
    type: string;
}

const RankChange = styled.div<RankChangeProps>`
display: inline-block;
background-color: rgba(0, 0, 0, 0.5);
padding: 5px;
width: 60px;
text-align: center;
${props => props.type === 'UP' ? css`color: lightgreen` : props.type === 'DOWN' ? css`color: red` : ''}
`