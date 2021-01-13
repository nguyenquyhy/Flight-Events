## Flight Events

![.NET Core](https://github.com/nguyenquyhy/Flight-Events/workflows/.NET%20Core/badge.svg)

This is a system to enhance the experience of flying in group with friends.

This includes:
- A server to handle 2-way communications. The web server also provides with a web interface to display all participating aircraft and upcoming events.
- A client to communicate with flight simulators via SimConnect and send data to the server.
- The same client can also serve as a simplified FSD server for ATC radar software such as Euroscope or AURORA.
- A Discord bot to automatically move players between voice channels based on their COM1 frequency. Another bot that can repeat ATIS info in voice channel.

### Discord Servers

For Discord communities that want to use Flight Events for their events, please check out the server guide [SERVER.md](SERVER.md) for instructions.

### Client Notes

- The client automatically forces single instance unless `--multiple-instances` flag is used at launch.

### Bot Notes

- Flight Events bot needs the following permissions:
  - Manage Channel (to create/remove frequency channels)
  - Read Text Channels & See Voice Channels
  - Send Messages (for ATIS function)
  - Embed Links (for ATIS function)
  - Connect (to clean up frequency channels)
  - Move Members
- ATIS bot needs the following permissions:
  - Connect
  - Speak
  - Use Voice Activity
  - Priority Speaker
- Sample bot link: https://discordapp.com/api/oauth2/authorize?client_id={BOT_CLIENT_ID}&scope=bot&permissions=221249553

### TODO

- [X] Show ATC on map
- [X] Show flight path trace
  - [X] Show multiple trace at the same time
- [X] Dynamic refresh rate
- [X] Dark mode on map
- [X] Show flight status on Discord
  - [X] Bot command !finfo
- [X] Colors for connection states
- [X] Teleport aircraft using map
- [X] x64 SimConnect from MSFS
- [X] Setting for minimize to Task bar
- [X] Stopwatch & leaderboard for race event
- [ ] Support for Events in client
  - [X] Show events and checklist
  - [ ] Notify button
  - [ ] Manual and Auto Refresh
- [X] Landing rate
  - [ ] Show landing rate on map
- [X] User database
  - [X] Mods & admins
- [ ] Private group
- [ ] Flight plan database for ATC
- [ ] Search airport on map
- [ ] MSIX packaging
- [ ] Gamebar integration
- [ ] Gradient for altitude
- [ ] 3D terrain