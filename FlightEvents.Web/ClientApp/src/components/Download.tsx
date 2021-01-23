import * as React from 'react';
import styled from 'styled-components';
import { Button, Modal, ModalHeader, ModalBody, Nav, NavItem, NavLink, TabContent, TabPane } from 'reactstrap';

type Tabs = 'Download' | 'Pilot' | 'ATC' | 'Discord' | 'Feedback';

const Download = () => {
    const [modal, setModal] = React.useState(false);
    const [activeTab, setActiveTab] = React.useState<Tabs>('Download');

    const toggle = () => setModal(!modal);

    return <>
        <Button color="primary" onClick={toggle} style={{ minWidth: '130px' }}>Join</Button>
        <Modal isOpen={modal} toggle={toggle} size="lg">
            <ModalHeader toggle={toggle}>Join Flight Events</ModalHeader>
            <ModalBody>
                <Nav tabs>
                    <NavItem>
                        <NavLink
                            className={activeTab === 'Download' ? 'active' : ''}
                            onClick={() => setActiveTab('Download')}>Download</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink
                            className={activeTab === 'Pilot' ? 'active' : ''}
                            onClick={() => setActiveTab('Pilot')}>Features for Pilot</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink
                            className={activeTab === 'ATC' ? 'active' : ''}
                            onClick={() => setActiveTab('ATC')}>Features for ATC</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink
                            className={activeTab === 'Discord' ? 'active' : ''}
                            onClick={() => setActiveTab('Discord')}>Features for Discord Server</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink
                            className={activeTab === 'Feedback' ? 'active' : ''}
                            onClick={() => setActiveTab('Feedback')}>Feedback</NavLink>
                    </NavItem>
                </Nav>
                <TabContent activeTab={activeTab}>
                    <TabPane tabId="Download">
                        <Wrapper>
                            <p>A client software needs to be run on your computer to connect Microsoft Flight Simulator to Flight Events.</p>

                            <a className="btn btn-primary" href="https://events-storage.flighttracker.tech/downloads/FlightEvents.Client.zip" target="_blank" rel="noopener noreferrer">Download FE Client for MSFS</a>

                            <p style={{ marginTop: 15 }}>Additionally, you can view and interact with Flight Events map directly inside the simulator with a toolbar panel.<br />Simply download, unzip and put folder "nguyenquyhy-ingamepanels-flightevents" in MSFS's Community folder.</p>
                            <a className="btn btn-primary" href="https://events-storage.flighttracker.tech/downloads/ingamepanels-flightevents.zip" target="_blank" rel="noopener noreferrer" style={{ borderBottomLeftRadius: 0, borderBottomRightRadius: 0 }}>Download MSFS Toolbar Panel</a>
                            <a href="https://events-storage.flighttracker.tech/downloads/ingamepanels-flightevents.zip" target="_blank" rel="noopener noreferrer">
                                <img style={{ maxWidth: '100%', marginBottom: 5, display: 'block' }} alt="Flight Events toolbar panel" src="https://events-storage.flighttracker.tech/images/InGamePanel.png" />
                            </a>

                            <hr />
                            <p>If you want to use Flight Events with FSX or P3D, you can try to use this legacy client (unsupported).</p>
                            <a className="btn btn-primary" href="https://events-storage.flighttracker.tech/downloads/FlightEvents.Client-FSX.zip" target="_blank" rel="noopener noreferrer">Download Legacy Client</a>
                            <p>Prerequisites: <a href="https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.7-windows-x86-installer" target="_blank" rel="noopener noreferrer">.NET Core Windows Runtime (x86)</a></p>
                        </Wrapper>
                    </TabPane>
                    <TabPane tabId="Pilot">
                        <Wrapper>
                            <h5>Flight Tracking</h5>
                            <p>Simply type in your preferred callsign and click Start Flight Tracking to show your aircraft on the map.</p>
                            <h5>ForeFlight Integration</h5>
                            <p>Tick "Broadcast data to local network", click "Start Flight Tracking" and you can follow your directly aircraft in <a href="https://www.foreflight.com/" target="_blank" rel="noopener noreferrer">ForeFlight</a>.</p>
                            <h5>Teleport To Anywhere</h5>
                            <p>Right click the map, click "Teleport aircraft here" to get a teleport code, then enter on the client to move your aircraft immediately to the location. You can also share the code with friends to get everyone together quickly.</p>
                            <h5>Discord Channel Switching <em>(in compatible Discord Servers)</em></h5>
                            <p>Flight Events can automatically switch you among frequency channels in participating Discord server.<br />You can enable this by connecting your Discord account in the client, starting flight tracking and connecting to the designated lounge Voice Channel in the Discord server.</p>
                            <h5>Other Features</h5>
                            <ul>
                                <li>Landing rate (and G-force) display</li>
                                <li>Discord Rich Present</li>
                            </ul>
                        </Wrapper>
                    </TabPane>
                    <TabPane tabId="ATC">
                        <Wrapper>
                            <p>The client can act as a FSD server for compatible ATC clients such as Euroscope and VRC.<br />You just need to click "Start ATC Server" (and VATSIM mode for VRC) and connect your favorite ATC client to localhost.</p>
                            <h5>Supporting ATC functions</h5>
                            <ul>
                                <li>Discord channel switching based on active frequency in your ATC client</li>
                                <li>See aircraft information and flight plans in your radar scope</li>
                                <li>Edit flight plans and assignments and sync the modifications to other controllers</li>
                                <li>Send message to other controllers</li>
                                <li>Get metar from NOAA</li>
                            </ul>
                            <h5>Partnership with ATConnect</h5>
                            <p>If you are interested in being a controller for community events and enhancing the realism of simming for other pilots, ATConnect is a great group to join!</p>
                            <a href="https://www.atconnect.de/" style={{ width: 300, backgroundColor: 'black', padding: 20, display: 'inline-block' }} target="_blank" rel="noopener noreferrer">
                                <img src="https://irp-cdn.multiscreensite.com/1d49ba89/dms3rep/multi/logo-with-white-text.svg" alt="ATConnect" />
                            </a>
                        </Wrapper>
                    </TabPane>
                    <TabPane tabId="Discord">
                        <Wrapper>
                            <p>Discord-related functions of Flight Events, including frequency channel switching and ATIS bots, can be added to any server.</p>
                            <p>You can follow our <a href="https://github.com/nguyenquyhy/Flight-Events/blob/master/SERVER.md" target="_blank" rel="noopener noreferrer">Server Guide</a> for setting up instructions.</p>
                        </Wrapper>
                    </TabPane>
                    <TabPane tabId="Feedback">
                        <Wrapper>
                            <p>If you have any feedbacks or suggestions, please let us know in our <a href="https://github.com/nguyenquyhy/Flight-Events/issues" target="_blank" rel="noopener noreferrer">GitHub Issues</a>.</p>
                            <img src="https://events-storage.flighttracker.tech/images/Thank you.png" style={{ width: 220 }} alt="Thank you!" />
                        </Wrapper>
                    </TabPane>
                </TabContent>
            </ModalBody>
        </Modal>
    </>
}

const Wrapper = styled.div`
margin-top: 10px;
`

export default Download;