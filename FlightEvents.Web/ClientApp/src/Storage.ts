import { MapTileType } from "./maps/IMap";

const PREF_KEY = "preferences.map";

export default class Storage {
    savePreferences(pref: Preferences) {
        return localStorage.setItem(PREF_KEY, JSON.stringify(pref));
    }
    loadPreferences() {
        const dataString = localStorage.getItem(PREF_KEY);
        if (dataString) {
            return JSON.parse(dataString) as Preferences;
        }
        return null;
    }
}

export interface Preferences {
    isDark: boolean;
    map3D: boolean;
    mapTileType: MapTileType;

    myClientId: string | null;
    followingClientId: string | null;
}