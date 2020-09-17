import 'bootstrap/dist/css/bootstrap.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { ApolloProvider, ApolloClient, InMemoryCache } from '@apollo/client';
import App from './App';
import registerServiceWorker from './registerServiceWorker';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

const client = new ApolloClient({
    uri: '/graphql',
    cache: new InMemoryCache({
        typePolicies: {
            FlightPlanData: {
                keyFields: ['title']
            },
            FlightPlanWaypoint: {
                keyFields: false
            }
        }
    })
});

ReactDOM.render(
    <ApolloProvider client={client}>
        <BrowserRouter basename={baseUrl}>
            <App />
        </BrowserRouter>
    </ApolloProvider>,
    rootElement);

registerServiceWorker();

