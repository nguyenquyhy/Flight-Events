var DEBUG = false;

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
        var m_Footer = document.querySelector("#Footer");
        m_Footer.classList.add("hidden");

        var iframe = document.querySelector("#CustomPanelIframe");

        var buttonFocus = document.querySelector("#ButtonFocus");
        buttonFocus.addEventListener("click", e => {
            this.loadMap(iframe);
        });

        this.loadMap(iframe);
    }

    loadMap(iframe) {
        var longitude = SimVar.GetSimVarValue("GPS POSITION LON", "degree longitude");
        var latitude = SimVar.GetSimVarValue("GPS POSITION LAT", "degree latitude");
        if (iframe) {
            iframe.src = "https://events.flighttracker.tech/?mode=MSFS&latitude=" + latitude + "&longitude=" + longitude;
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