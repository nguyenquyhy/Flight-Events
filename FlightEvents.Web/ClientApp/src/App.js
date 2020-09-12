import React, { Component } from 'react';
import { Route } from 'react-router';
import { Home } from './components/Home';
import Stopwatch from './components/Stopwatch';

import './custom.css'

export default class App extends Component {
    static displayName = App.name;

    render() {
        return (
            <>
                <Route exact path='/' component={Home} />
                <Route exact path='/Stopwatch/:eventCode' component={Stopwatch} />
            </>
        );
    }
}
