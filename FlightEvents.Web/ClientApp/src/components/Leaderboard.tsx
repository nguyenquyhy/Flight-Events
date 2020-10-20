import * as React from 'react';
import styled from 'styled-components';
import { FlightEvent, LeaderboardRecord } from '../Models';

interface LeaderboardProps {
    event: FlightEvent;
    leaderboards: Leaderboards;
}

export type Leaderboards = { [name: string]: { [subIndex: number]: LeaderboardRecord[] } };

export const recordsToLeaderboards = (records: LeaderboardRecord[]) => {
    return records.reduce((prev, curr) => {
        prev[curr.leaderboardName] =
            prev[curr.leaderboardName] ?
                {
                    ...prev[curr.leaderboardName],
                    [curr.subIndex]:
                        (prev[curr.leaderboardName][curr.subIndex]
                            ? prev[curr.leaderboardName][curr.subIndex].concat([curr]) : [curr])
                } :
                {
                    [curr.subIndex]: [curr]
                };
        return prev;
    }, {} as Leaderboards)
}

const Leaderboard = (props: LeaderboardProps) => {
    const milestones = ['Full race'];
    if (props.event.leaderboardLaps) {
        for (let i = 0; i < props.event.leaderboardLaps.length; i++) {
            milestones.push((i === 0 ? 'Start' : props.event.leaderboardLaps[i - 1]) + ' → ' + props.event.leaderboardLaps[i]);
        }
    }

    return <div className="table-responsive">
        <table className="table table-bordered">
            <thead>
                <tr>
                    <th rowSpan={2}></th>
                    {!!props.event.leaderboards && props.event.leaderboards.map(leaderboardName => <th key={leaderboardName} colSpan={3}>{leaderboardName}</th>)}
                </tr>
                <tr>
                    {!!props.event.leaderboards && props.event.leaderboards.map(leaderboardName => <React.Fragment key={leaderboardName}><th>Pos</th><th>Name</th><th>Time</th></React.Fragment>)}
                </tr>
            </thead>
            <tbody>
                {milestones.map((milestoneName, subIndex) => <tr key={subIndex}>
                    <td>{milestoneName}</td>
                    {!!props.event.leaderboards && props.event.leaderboards.map(leaderboardName => {
                        let leaderboard = props.leaderboards[leaderboardName];
                        if (!leaderboard) return <React.Fragment key={leaderboardName}><td></td><td></td><td></td></React.Fragment>;

                        let records = leaderboard[subIndex];
                        if (!records) return <React.Fragment key={leaderboardName}><td></td><td></td><td></td></React.Fragment>;

                        records = records.sort((a, b) => b.score - a.score);
                        return <React.Fragment key={leaderboardName}>
                            <td>
                                {records.map((_, index) => <div key={index}>{index + 1}</div>)}
                            </td>
                            <td>
                                {records.map((record, index) => <StyledName key={index} title={(record.playerName + '\n' + record.scoreDisplay)}>{record.playerName}</StyledName>)}
                            </td>
                            <td>
                                {records.map((record, index) => <StyledTime key={index} title={(record.playerName + '\n' + record.scoreDisplay)}>{record.scoreDisplay}</StyledTime>)}
                            </td>
                        </React.Fragment>
                    })}
                </tr>)}
            </tbody>
        </table>
    </div>;
}

const StyledName = styled.div`
white-space: nowrap;
overflow: hidden;
text-overflow: ellipsis;
`

const StyledTime = styled.div`
`

export default Leaderboard;