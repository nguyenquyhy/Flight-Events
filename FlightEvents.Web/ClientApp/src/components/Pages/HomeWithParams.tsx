import * as React from 'react';
import * as signalr from '@microsoft/signalr';
import * as protocol from '@microsoft/signalr-protocol-msgpack';
import { omit } from 'lodash';
import { MapDimension, MapMode, MapPosition, MapTileType, View } from '../../maps/IMap';
import AircraftList, { AircraftStatusInList } from '../AircraftList';
import ControllerList from '../ControllerList';
import FlightPlanLoader from '../FlightPlanLoader';
import MapComponent from '../MapComponent';
import TeleportDialog from '../Dialogs/TeleportDialog';
import Display from '../Display';
import EventList from '../EventList';
import Hud from '../Hud';
import Ruler from '../Ruler';
import UserIcon from '../UserIcon';
import { AircraftStatus, AircraftStatusBrief, Airport, ATCInfo, ATCStatus, FlightPlan, FlightPlanData } from '../../Models';
import { convertPropertyNames, pascalCaseToCamelCase } from '../../Converters';
import PreferenceStorage from '../../PreferenceStorage';
import { useStateFromProps } from '../../Hooks';
import TeleportButton from '../TeleportButton';

const CONTROLLER_TIMEOUT_MILLISECONDS = 30000;
const AIRCRAFT_TIMEOUT_MILLISECONDS = 10000;

interface HomeWithParamsProps {
    storage: PreferenceStorage;

    mode: MapMode | null;
    eventId: string | null;
    initialView: View;
    focusView: View | null;
    callsignFilter: string[] | null;

    isDark: boolean;
    mapDimension: MapDimension;
    mapTileType: MapTileType;

    group: string | null;
    myClientId: string | null;
    followingClientId: string | null;
    showPlanClientId: string | null;
    showRouteClientIds: string[];

    onCallsignReceived: (clientId: string, callsign: string, status: AircraftStatus | null) => void;
}

const hub = new signalr.HubConnectionBuilder()
    .withUrl('/FlightEventHub?clientType=Web')
    .withAutomaticReconnect()
    .withHubProtocol(new protocol.MessagePackHubProtocol())
    .build();

const HomeWithParams = (props: HomeWithParamsProps) => {
    const [isDark, setIsDark] = React.useState(props.isDark);
    const [mapDimension, setMapDimension] = React.useState(props.mapDimension);
    const [mapTileType, setMapTileType] = React.useState(props.mapTileType);

    const [myClientId, setMyClientId] = useStateFromProps<string | null>(props.myClientId);
    const [followingClientId, setFollowingClientId] = useStateFromProps<string | null>(props.followingClientId);
    const [flightPlanClientId, setFlightPlanClientId] = useStateFromProps<string | null>(props.showPlanClientId);
    const [showRouteClientIds, setShowRouteClientIds] = useStateFromProps<string[]>(props.showRouteClientIds);
    const [showInfoClientIds, setMoreInfoClientIds] = React.useState<string[]>([]);

    const [prependingRoute, setPrependingRoute] = React.useState<{ clientId: string, route: AircraftStatusBrief[] } | null>(null);

    const [aircrafts, setAircrafts] = React.useState<{ [clientId: string]: AircraftStatusInList }>({});
    const [controllers, setControllers] = React.useState<{ [clientId: string]: ATCInfo & ATCStatus & { lastUpdated: Date } }>({});

    const [teleportPosition, setTeleportPosition] = React.useState<MapPosition | null>(null);

    const [drawingFlightPlans, setDrawingFlightPlans] = React.useState<FlightPlanData[]>([]);
    const [drawingAirports, setDrawingAirports] = React.useState<Airport[]>([]);

    const [focusView, setFocusView] = useStateFromProps<View | null>(props.focusView);
    const [isDrawing, setIsDrawing] = React.useState(false);
    const [isTeleporting, setIsTeleporting] = React.useState(false);

    const aircraftStatusCache = React.useRef<{ [key: string]: { lastUpdated: Date, status: AircraftStatus } }>({})

    const cleanUp = React.useCallback(() => {
        for (let clientId of Object.keys(controllers)) {
            const controller = controllers[clientId];
            if (new Date().getTime() - controller.lastUpdated.getTime() > CONTROLLER_TIMEOUT_MILLISECONDS) {
                cleanUpController(clientId);
            }
        }

        for (let [clientId, aircraft] of Object.entries(aircraftStatusCache.current)) {
            if (new Date().getTime() - aircraft.lastUpdated.getTime() > AIRCRAFT_TIMEOUT_MILLISECONDS) {
                /// NOTE: we do not clean up Own, Follow, Flight Plan, More Info and Show Route selection here to allow restoring on reconnection
                setAircrafts(aircrafts => omit(aircrafts, clientId));
            }
        }
    }, [controllers]);

    const cleanUpController = (clientId: string) => {
        setControllers(controllers => omit(controllers, clientId));
    }

    const handleControllerClick = React.useCallback((clientId: string) => {
        setFocusView(controllers[clientId]);
    }, [controllers, setFocusView]);

    const handleAircraftClick = React.useCallback((clientId: string) => {
        setFocusView(aircraftStatusCache.current[clientId].status);
    }, [setFocusView]);

    const handleIsDarkChanged = (isDark: boolean) => {
        setIsDark(isDark);
    }

    const handleMapDimensionChanged = (dimension: MapDimension) => {
        setMapDimension(dimension);
    }

    const handleTileTypeChanged = (tileType: MapTileType) => {
        setMapTileType(tileType);
    }

    const handleMeChanged = React.useCallback((clientId: string | null) => {
        setMyClientId(clientId);
    }, [setMyClientId]);

    const handleShowRouteChanged = React.useCallback((clientId: string) => {
        if (showRouteClientIds.includes(clientId)) {
            // Remove
            setShowRouteClientIds(showRouteClientIds.filter(o => o !== clientId));
        } else {
            // Add
            setShowRouteClientIds(showRouteClientIds.concat(clientId));
        }
    }, [showRouteClientIds, setShowRouteClientIds]);

    const handleRequestFlightRoute = React.useCallback((clientId: string) => {
        let route: AircraftStatusBrief[] = [];
        hub.stream("RequestFlightRoute", clientId)
            .subscribe({
                next: item => {
                    route = [item].concat(route);
                },
                complete: () => {
                    route = convertPropertyNames(route, pascalCaseToCamelCase) as AircraftStatusBrief[];
                    setPrependingRoute({ clientId, route });
                },
                error: () => {

                }
            });
    }, []);

    const handleFollowingChanged = React.useCallback((clientId: string | null) => {
        setFollowingClientId(clientId);
    }, [setFollowingClientId]);

    const handleShowInfoChanged = React.useCallback((clientId: string) => {
        if (showInfoClientIds.includes(clientId)) {
            setMoreInfoClientIds(showInfoClientIds.filter(o => o !== clientId));
        } else {
            setMoreInfoClientIds(showInfoClientIds.concat(clientId));
        }
    }, [showInfoClientIds]);

    const handleFlightPlanChanged = React.useCallback((clientId: string | null) => {
        setFlightPlanClientId(clientId);
    }, [setFlightPlanClientId]);

    const handleAirportsLoaded = React.useCallback((airports: Airport[]) => {
        setDrawingAirports(airports);
    }, [setDrawingAirports]);

    const handleFlightPlansLoaded = React.useCallback((flightPlans: FlightPlan[]) => {
        setDrawingFlightPlans(flightPlans.map(o => o.data));
    }, [setDrawingFlightPlans]);

    const handleTeleportPositionSelected = (position: MapPosition) => {
        setTeleportPosition(position);
    }

    const handleTeleportRequested = React.useCallback((code: string, position: MapPosition, altitude: number) => {
        hub.send('RequestTeleport', code, position.latitude, position.longitude, altitude);
    }, []);

    const handleTeleportCompleted = React.useCallback(() => {
        setTeleportPosition(null);
    }, []);

    const handleStartDrawing = () => {
        setIsDrawing(true);
    }

    const handleDrawingCompleted = () => {
        setIsDrawing(false);
    }

    const handleStartTeleporting = () => {
        setIsTeleporting(true);
    }

    const handleTeleportingCompleted = () => {
        setIsTeleporting(false);
    }

    React.useEffect(() => {
        const handler = (message) => {
            if (message.origin === 'coui://html_ui') {
                setFocusView({ longitude: message.data.longitude, latitude: message.data.latitude, zoom: 13 });
            }
        };

        window.addEventListener('message', handler);

        return () => {
            window.removeEventListener('message', handler);
        }
    }, [setFocusView]);

    const { onCallsignReceived, callsignFilter } = props;

    React.useEffect(() => {
        const handler = (clientId, aircraftStatus: AircraftStatus) => {
            aircraftStatus = convertPropertyNames(aircraftStatus, pascalCaseToCamelCase);

            if (callsignFilter && !callsignFilter.includes(aircraftStatus.callsign)) {
                return
            }

            try {
                const isReady = !(Math.abs(aircraftStatus.latitude) < 0.02 && Math.abs(aircraftStatus.longitude) < 0.02);

                setAircrafts(aircrafts => {
                    if (aircrafts[clientId] &&
                        aircrafts[clientId].isReady === isReady &&
                        aircrafts[clientId].callsign === aircraftStatus.callsign &&
                        aircrafts[clientId].group === aircraftStatus.group) {
                        return aircrafts;
                    }
                    return {
                        ...aircrafts,
                        [clientId]: {
                            isReady: isReady,
                            callsign: aircraftStatus.callsign,
                            group: aircraftStatus.group
                        }
                    }
                });

                aircraftStatusCache.current[clientId] = {
                    lastUpdated: new Date(),
                    status: aircraftStatus
                }

                onCallsignReceived(clientId, aircraftStatus.callsign, isReady ? aircraftStatus : null);

            } catch (e) {
                console.error(e);
            }
        };

        hub.on("UpdateAircraft", handler);

        return () => {
            hub.off("UpdateAircraft", handler);
        }
    }, [onCallsignReceived, followingClientId, callsignFilter]);

    React.useEffect(() => {
        hub.onreconnected(async connectionId => {
            console.log('Connected to SignalR with connection ID ' + connectionId);

            await hub.send('Join', 'Map');
        })

        hub.on("UpdateATC", (clientId, status: ATCStatus, atc: ATCInfo) => {
            console.log("Update ATC")
            status = convertPropertyNames(status, pascalCaseToCamelCase);
            atc = convertPropertyNames(atc, pascalCaseToCamelCase);

            try {
                if (atc && status) {
                    setControllers(controllers => ({
                        ...controllers,
                        [clientId]: {
                            lastUpdated: new Date(),
                            ...atc,
                            ...status
                        }
                    }));
                } else {
                    // Remove
                    cleanUpController(clientId);
                }
            } catch (e) {
                console.error(e);
            }
        });

        hub.on("ReturnFlightPlanDetails", (connectionId, flightPlan: FlightPlanData | null) => {
            if (flightPlan) {
                flightPlan = convertPropertyNames(flightPlan, pascalCaseToCamelCase) as FlightPlanData;
                setDrawingFlightPlans([flightPlan]);
            }
        });

        const f = async () => {
            await hub.start();
        }
        f();

        return () => {
            hub.off("UpdateATC");
            hub.off("ReturnFlightPlanDetails");
            hub.stop();
        }
    }, []);

    React.useEffect(() => {
        if (flightPlanClientId !== null) {
            // Request plan
            hub.send('RequestFlightPlanDetails', flightPlanClientId);
        }
    }, [flightPlanClientId]);

    React.useEffect(() => {
        const timeout = setInterval(cleanUp, 2000);
        return () => {
            clearInterval(timeout);
        }
    }, [cleanUp]);

    React.useEffect(() => {
        props.storage.savePreferences({
            isDark: isDark,
            map3D: mapDimension === MapDimension._3D,
            mapTileType: mapTileType,

            myClientId: myClientId,
            followingClientId: followingClientId,
        });
    }, [props.storage, isDark, mapDimension, mapTileType, myClientId, followingClientId]);

    return <>
        <MapComponent
            hub={hub}

            mode={props.mode} initialView={props.initialView}
            isDark={isDark} dimension={mapDimension} tileType={mapTileType}

            group={props.group}
            allClientIds={Object.keys(aircrafts)}
            myClientId={myClientId} onMeChanged={handleMeChanged}
            followingClientId={followingClientId} onFollowingChanged={handleFollowingChanged}
            flightPlanClientId={flightPlanClientId} onShowPlanChanged={handleFlightPlanChanged}

            showRouteClientIds={showRouteClientIds}
            onShowRouteChanged={handleShowRouteChanged}
            onRequestFlightRoute={handleRequestFlightRoute}
            prependingRoute={prependingRoute}

            showInfoClientIds={showInfoClientIds}
            onShowInfoChanged={handleShowInfoChanged}
            onTeleportPositionSelected={handleTeleportPositionSelected}

            flightPlans={drawingFlightPlans}
            airports={drawingAirports}
            controllers={controllers}

            focusView={focusView}
            callsignFilter={props.callsignFilter}

            isDrawing={isDrawing}
            onDrawingCompleted={handleDrawingCompleted}

            isTeleporting={isTeleporting}
            onTeleportingCompleted={handleTeleportingCompleted}
            />

        {props.mode !== MapMode.none && <>
            <Display
                mode={props.mode}
                isDark={isDark} onIsDarkChanged={handleIsDarkChanged}
                dimension={mapDimension} onDimensionChanged={handleMapDimensionChanged}
                tileType={mapTileType} onTileTypeChanged={handleTileTypeChanged} />

            {props.mapDimension === MapDimension._2D && <Ruler isDrawing={isDrawing} onStartDrawing={handleStartDrawing} />}

            {props.mapDimension === MapDimension._2D && <TeleportButton isTeleporting={isTeleporting} onStartTeleporting={handleStartTeleporting} />}

            <Hud
                mode={props.mode}
                aircrafts={Object.entries(aircrafts).reduce((prev, [key, value]) => ({
                    ...prev,
                    [key]: value.callsign
                }), {})}
                onAircraftClick={handleAircraftClick}
                myClientId={myClientId} onMeChanged={handleMeChanged}
                followingClientId={followingClientId} onFollowingChanged={handleFollowingChanged}
                flightPlanClientId={flightPlanClientId} onFlightPlanChanged={handleFlightPlanChanged}
            />

            <ControllerList controllers={controllers} onControllerClick={handleControllerClick} />

            <AircraftList
                aircrafts={aircrafts} onAircraftClick={handleAircraftClick}
                group={props.group}
                myClientId={myClientId}
                followingClientId={followingClientId}
                flightPlanClientId={flightPlanClientId}
                showRouteClientIds={showRouteClientIds} onShowRouteChanged={handleShowRouteChanged}
                moreInfoClientIds={showInfoClientIds} onMoreInfoChanged={handleShowInfoChanged}
            />

            <EventList hub={hub} onAirportsLoaded={handleAirportsLoaded} onFlightPlansLoaded={handleFlightPlansLoaded} />
        </>}

        {!!props.eventId && <FlightPlanLoader eventId={props.eventId} onFlightPlansLoaded={handleFlightPlansLoaded} onAirportsLoaded={handleAirportsLoaded} />}

        <TeleportDialog selectedPosition={teleportPosition} onRequested={handleTeleportRequested} onComplete={handleTeleportCompleted} />

        <UserIcon />
    </>;
}

export default HomeWithParams;