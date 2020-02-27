import { FlightEvent } from './Models';

class Api {
    public async getFlightEvents() {
        const query = `{
    flightEvents {
        id
        name
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
    }
}`;
        const data = await this.graphQLQueryAsync(query, { id: id });
        return data.flightEvent as FlightEvent;
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