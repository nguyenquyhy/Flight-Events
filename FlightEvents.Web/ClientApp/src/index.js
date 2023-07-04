import 'bootstrap/dist/css/bootstrap.css';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { ApolloProvider, ApolloClient, InMemoryCache } from '@apollo/client';
import * as serviceWorkerRegistration from './serviceWorkerRegistration';
import App from './App';

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

serviceWorkerRegistration.register({
    onUpdate: (registration) => {
        var element = document.getElementById("divUpdateMsg");
        element.innerHTML =
            "An updated version of this website is available. Please reload the page.";
        element.style.display = "block";

        if (registration && registration.waiting) {
            // Skip waiting to remove the requirement to restart browser
            registration.waiting.postMessage({ type: "SKIP_WAITING" });
        }
    }
})