## Server Guide

Although Flight Events and many of its features were first introduced in the MSFS Discord Server (even before it became official server), it was meant to be usable by any communities.

Right now you can add all our bots to your Discord server for the frequency switching and ATIS functionality by following the Bot Setup section below. For all other event-related functions, feel free to PM me on Discord and have a chat.

### Highlighted functions

- Flight Events bot can automatically switch pilots and ATCs among voice channels based on their frequency in MSFS or Euroscope/VRC.
- ATIS bot can loop a provided MP3 file in a voice channel.
- Checklist inside Flight Events client to provide information about events
- Live stopwatch, leaderboard and stream overlay are available for racing events (or anything similar)

### Event Lists

To show your events on the home page and FE client, or access event related function such as stopwach, leaderboard and stream overlay, please PM me on Discord. 

My plan is to open this up to the community with some light moderation. Unfortunately I have not finished implementing all the necessary user interface yet.

### Bot Setup

1. Add the bots to your server

Flight Events bot
https://discordapp.com/api/oauth2/authorize?client_id=688293497748455489&scope=bot

ATIS bots (you can skip those if you don't want ATIS function)
https://discordapp.com/api/oauth2/authorize?client_id=758367454228774932&scope=bot
https://discordapp.com/api/oauth2/authorize?client_id=758560795708751892&scope=bot
https://discordapp.com/api/oauth2/authorize?client_id=758561335624859658&scope=bot

2. Create a channel category in Discord and a lounge voice channel in that category

Flight Events bot will manage all voice channels in that category, including automatically creating new frequency channels for both pilots and ATCs, and removing unused channels (except the lounge) after a short period of being empty.

Since the bot cannot directly connect people to a voice channel and can only move people among voice channels, the lounge is needed as a starting point for anyone wanting to use the channel switching function. The bot will also move people back to the lounge once they exit the sim.

*Notes:*
- Flight Event bot won't add/remove text channels.
- If you want to retains voice channels other than the lounge, you can add those channels to the whitelist. Flight Events won't remove those channels, but won't move people from/to those channels either.

3. Create a text channel for command if you want to use ATIS function

This channel does not have to in the category created above. It can be anywhere Flight Events bot has access to.

4. Pm me on Discord with your server ID, category ID, lounge channel ID and command channel ID (if used)

There are also some additional settings that your might be interested in:
- Whitelisted channel ID (described above)
- Channel bitrate: you can specify the bitrate of the frequency channels the bot creates
- Channel name suffix: you can add a suffix (e.g. ` (enable PTT to talk)`) to the name of the frequency channels the bot creates

5. Set the permissions of the channels and category

**Server:**

*For ATIS bots:*
- Change nickname: Allow

**Category:** Permission in the category will be inheritted by any frequency channels created by the bot, so by setting the permission at the category, you actually make a permission preset for all the frequency channels.

*For `@everyone`:*
- Connect: you can Deny if you want to enforce switching by the bot. Otherwise you can leave it as default or set to Allow to allow switching manually by clicking in Discord.
- Speak: inherit or Allow
- Use Voice Activity: you should set this to Deny

*For ATCs and Mods:*
- Use Voice Activity: you can set this to Allow if you want to allow ATCs and Mods to not use PTT
- Priority Speaker: you should set this to Allow

*For Flight Events bot:*
- View channel: you should set this to Allow. Sometimes due to server setup to hide channels until clicking some emote, the bot cannot see any channels in the server without this permission.
- Manage channel: Allow so that the bot can automatically create/delete voice channels
- Connect: Allow because the bot can only move people to channel it can connect to. This is also needed to allow the bot to clean up empty frequency channels.
- Move Members: Allow

*For ATIS bots:*
- View channel: you should set this to Allow. Sometimes due to server setup to hide channels until clicking some emote, the bot cannot see any channels in the server without this permission.
- Change nickname: you should set this to Allow. ATIS bot will change its nickname to what you specify in the command.
- Connect: Allow
- Speak: Allow
- Priority Speaker: Allow

**Lounge Channel:**

*For `@everyone`:*
- Connect: Allow
- Speak: inherit or Allow
- Use Voice Activity: you may want to set Allow here if you want to overwrite the Deny in the category.

**Command Channel:** this should be a private channel that can be read & write by admins/mods and Flight Event bot

*For Flight Events bot:*
- View channel: Allow
- Send messages: Allow so that the bot can respond to your command
- Embed links: Allow so that the bot can show you the status of the ATIS bot

### ATIS Bot Commands

ATIS function accepts some commands in a predefined command channel:

`!atis <frequency> <name>` (e.g. `!atis 123.4 KJFK ATIS`)

This command must be put as a description of a uploading MP3 file. This will automatically create a frequency channel as specified; one of the ATIS bot will change it nickname to the specified name, join the frequency channel and start looping the provided MP3.

Since there are only 3 ATIS bots at the moment, you can only have 3 ATIS running at the same time. If you need more for your events, please let me know.

`!atis stop <frequency>`

Stop the ATIS bot in a particular frequency. The channel will be cleaned up automatically by Flight Events bot when it is empty.
