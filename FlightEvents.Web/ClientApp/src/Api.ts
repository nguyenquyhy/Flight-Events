import { FlightEvent, Airport, FlightPlan } from './Models';

class Api {
    public async getFlightEvents() {
        const query = `{
    flightEvents {
        id
        name
        startDateTime
    }
}`;
        const data = await this.graphQLQueryAsync(query);
        return data.flightEvents as FlightEvent[];
    }

    public async getFlightEvent(id: string) {
        const query = `query ($id: Uuid!) {
    flightEvent(id: $id) {
        id
        name
        description
        startDateTime
        url
        waypoints
    }
}`;
        const data = await this.graphQLQueryAsync(query, { id: id });
        return data.flightEvent as FlightEvent;
    }

    public async getAirports(idents: string[]) {
        const query = `query ($idents: [String]!) {
    airports(idents: $idents) {
        ident
        name
        longitude
        latitude
    }
}`;
        const data = await this.graphQLQueryAsync(query, { idents: idents });
        return data.airports as Airport[];
    }

    public async getFlightPlans(eventId: string) {
        const query = `query ($id: Uuid!) {
    flightEvent(id: $id) {
        flightPlans {
            id
            downloadUrl
            data {
                title
                description
                cruisingAltitude
                type
                routeType
                departure {
                    id
                    name
                    latitude
                    longitude
                }
                destination {
                    id
                    name
                    latitude
                    longitude
                }
                waypoints {
                    id
                    airway
                    type
                    latitude
                    longitude
                    icao {
                        ident
                        airport
                        region
                    }
                }
            }
        }
    }
}`;
        const data = await this.graphQLQueryAsync(query, { id: eventId });
        return data.flightEvent.flightPlans as FlightPlan[];
    }

    private async graphQLQueryAsync(query: string, variables?: any) {
        const response = await fetch('graphql', {
            method: 'post',
            body: JSON.stringify({
                query: query,
                variables: variables
            }),
            headers: {
                'Content-Type': 'application/json'
            }
        });


        if (response.ok) {
            const data = await response.json();
            return data.data;
        } else {
            throw new Error("Cannot get data!");
        }
    }
}

export default new Api();