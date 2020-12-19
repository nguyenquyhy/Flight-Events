import React, { Component } from 'react';
import { Route } from 'react-router';
import { Home } from './components/Pages/Home';
import EventListPage from './components/Pages/EventListPage';
import EventDetailsPage from './components/Pages/EventDetailsPage';
import StopwatchPage from './components/Pages/StopwatchPage';
import LeaderboardPage from './components/Pages/LeaderboardPage';
import OverlayTime from './components/Pages/OverlayTime';
import OverlayLiveLeaderboard from './components/Pages/OverlayLiveLeaderboard';
import FlightPlanCreatePage from './components/Pages/FlightPlanCreatePage';
import FlightPlanListPage from './components/Pages/FlightPlanListPage';

import './custom.css'

export default class App extends Component {
    static displayName = App.name;

    render() {
        return (
            <>
                <Route exact path='/' component={Home} />
                <Route exact path='/Events' component={EventListPage} />
                <Route exact path='/Events/:id' component={EventDetailsPage} />
                <Route exact path='/Events/:id/Stopwatch' component={StopwatchPage} />
                <Route exact path='/Events/:id/Leaderboard' component={LeaderboardPage} />
                <Route exact path='/Events/:id/Overlay/Time' component={OverlayTime} />
                <Route exact path='/Events/:id/Overlay/LiveLeaderboard' component={OverlayLiveLeaderboard} />
                <Route exact path='/FlightPlans/Create' component={FlightPlanCreatePage} />
                <Route exact path='/FlightPlans' component={FlightPlanListPage} />
            </>
        );
    }
}
