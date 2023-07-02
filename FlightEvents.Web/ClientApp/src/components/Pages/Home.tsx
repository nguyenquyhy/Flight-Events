import * as React from 'react';
import 'msgpack5';
import { RouteComponentProps } from 'react-router-dom';
import PreferenceStorage from '../../PreferenceStorage';
import HomeWithParams from './HomeWithParams';
import { MapDimension, MapMode, MapTileType, View } from '../../maps/IMap';
import { AircraftStatus } from '../../Models';
import { useStateOnce } from '../../Hooks';

type Props = RouteComponentProps<any>;

const storage = new PreferenceStorage();

const Home = (props: Props) => {
    const pref = storage.loadPreferences();

    const searchParams = new URLSearchParams(props.location.search);

    const mode = searchParams.get('mode');
    const theme = searchParams.get('theme');

    const group = searchParams.get('group');
    const myCallsign = searchParams.get('myCallsign');
    const followCallsign = searchParams.get('followCallsign');
    const showPlanCallsign = searchParams.get('showPlanCallsign');
    const showRouteCallsign = searchParams.get('showRouteCallsign');
    const focusCallsign = searchParams.get('focusCallsign');

    const latitude = searchParams.get('latitude') ? Number(searchParams.get('latitude')) : null;
    const longitude = searchParams.get('longitude') ? Number(searchParams.get('longitude')) : null;
    const zoom = Number(searchParams.get('zoom'));
    const scaling = Number(searchParams.get('scaling'));

    const eventId = searchParams.get('eventId');
    const callsignFilter = searchParams.get('callsigns')?.split(',') || null;

    const showEvents = searchParams.get('showEvents');

    const panelVersion = searchParams.get('version');
    if (panelVersion) {
        const elem = document.getElementById('divUpdateMsg');
        if (elem) {
            elem.innerHTML = panelVersion;
            elem.style.display = 'block';
        }
    }

    const initialView = React.useRef({
        latitude: latitude, longitude: longitude, zoom: zoom, scaling: scaling
    });

    const [focusView, setFocusView] = React.useState<View | null>(null);

    if (mode === "MSFS" && pref && pref.map3D) {
        pref.map3D = false;
    }

    const [, setFocusClientId] = useStateOnce<string | null>(null);
    const [myClientId, setMyClientId] = useStateOnce(pref ? pref.myClientId : null);
    const [followingClientId, setFollowingClientId] = useStateOnce(pref ? pref.followingClientId : null);
    const [showPlanClientId, setShowPlanClientId] = useStateOnce<string | null>(null);
    const [showRouteClientIds, setShowRouteClientIds] = useStateOnce<string[]>([]);

    const handleCallsignReceived = (clientId: string, callsign: string, status: AircraftStatus | null) => {
        // Set focus aircraft from URL
        if (focusCallsign && callsign === focusCallsign) {
            if (setFocusClientId(clientId) && status) {
                setFocusView(status);
            }
        }
        // Set own aircraft from URL
        if (myCallsign && callsign === myCallsign) {
            if (setMyClientId(clientId) && status) {
                setFocusView(status);
            }
        }
        // Set follow aircraft from URL
        //else
        if (followCallsign && callsign === followCallsign) {
            setFollowingClientId(clientId);
        }
        // Set show plan from URL
        if (showPlanCallsign && callsign === showPlanCallsign) {
            setShowPlanClientId(clientId);
        }
        // Set show route from URL
        if (showRouteCallsign && callsign === showRouteCallsign) {
            setShowRouteClientIds([clientId]);
        }
    }

    return <HomeWithParams
        storage={storage}
        mode={(mode ? MapMode[mode] : null)}
        eventId={eventId}
        initialView={initialView.current}
        focusView={focusView}
        callsignFilter={callsignFilter}
        isDark={theme === 'dark' ? true : (pref ? pref.isDark : false)}
        mapDimension={pref && pref.map3D ? MapDimension._3D : MapDimension._2D}
        mapTileType={pref ? pref.mapTileType : MapTileType.OpenStreetMap}
        group={group}
        myClientId={myClientId}
        followingClientId={followingClientId}
        showPlanClientId={showPlanClientId}
        showRouteClientIds={showRouteClientIds}
        isShowingEvents={showEvents !== 'none'}
        onCallsignReceived={handleCallsignReceived}
    />
}

export default Home;