import React, { Component } from 'react';
import { Route } from 'react-router';
import { Home } from './components/Pages/Home';
import StopwatchPage from './components/Pages/StopwatchPage';
import FlightPlanCreatePage from './components/Pages/FlightPlanCreatePage';
import FlightPlanListPage from './components/Pages/FlightPlanListPage';

import './custom.css'

export default class App extends Component {
    static displayName = App.name;

    render() {
        return (
            <>
                <Route exact path='/' component={Home} />
                <Route exact path='/Stopwatch/:eventCode' component={StopwatchPage} />
                <Route exact path='/FlightPlans/Create' component={FlightPlanCreatePage} />
                <Route exact path='/FlightPlans' component={FlightPlanListPage} />
            </>
        );
    }
}
