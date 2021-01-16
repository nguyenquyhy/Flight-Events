var DEBUG = false;
var VERSION = "0.3.0";
var BASE_URL = "https://events.flighttracker.tech";
//var BASE_URL = "https://localhost:44359";

class IngamePanelFlightEventsPanel extends HTMLElement {

    connectedCallback() {
        Include.addScript("/JS/simvar.js", () => {
            if (DEBUG) {
                setTimeout(() => {
                    this.enableDebug();
                }, 1000);
            } else {
                setTimeout(() => {
                    this.initialize();
                }, 1000);
            }
        });
    }

    initialize() {
        var iframe = document.querySelector("#CustomPanelIframe");

        ButtonFocus.addEventListener("click", e => {
            var longitude = SimVar.GetSimVarValue("GPS POSITION LON", "degree longitude");
            var latitude = SimVar.GetSimVarValue("GPS POSITION LAT", "degree latitude");
            iframe.contentWindow.postMessage({ longitude: longitude, latitude: latitude }, "*");
        });

        this.loadMap(iframe);
    }

    loadMap(iframe) {
        var longitude = SimVar.GetSimVarValue("GPS POSITION LON", "degree longitude");
        var latitude = SimVar.GetSimVarValue("GPS POSITION LAT", "degree latitude");
        if (iframe) {
            iframe.src = `${BASE_URL}/?mode=MSFS&latitude=${latitude}&longitude=${longitude}&version=${VERSION}`;
        }
    }

    enableDebug() {
        if (typeof g_modDebugMgr != "undefined") {
            this.initialize();
            this.addDebugControls()
        }
        else {
            Include.addScript("/JS/debug.js", () => {
                if (typeof g_modDebugMgr != "undefined") {
                    this.initialize();
                    this.addDebugControls();
                } else {
                    setTimeout(() => {
                        this.enableDebug();
                    }, 2000);
                }
            });
        }
    }

    addDebugControls() {
        g_modDebugMgr.AddConsole(null);
        g_modDebugMgr.AddDebugButton("Source", () => {
            console.log('Source');
            console.log(window.document.documentElement.outerHTML);
        });
    }
}
window.customElements.define("ingamepanel-flightevents", IngamePanelFlightEventsPanel);
checkAutoload();