import 'bootstrap/dist/css/bootstrap.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import ApolloClient from 'apollo-boost';
import { ApolloProvider } from 'react-apollo';
import { InMemoryCache } from 'apollo-cache-inmemory';
import App from './App';
import registerServiceWorker from './registerServiceWorker';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

const cache = new InMemoryCache();
const client = new ApolloClient({
    cache
});

ReactDOM.render(
    <ApolloProvider client={client}>
        <BrowserRouter basename={baseUrl}>
            <App />
        </BrowserRouter>
    </ApolloProvider>,
    rootElement);

registerServiceWorker();

