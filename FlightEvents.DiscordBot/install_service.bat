SET mypath=%~dp0
sc create FlightEvents.DiscordBot binPath="%mypath%FlightEvents.DiscordBot.exe" start=delayed-auto DisplayName="Flight Events Discord Bot"
sc description FlightEvents.DiscordBot "A Discord bot to switch user's voice channel based on COM1 frequency in the sim."
sc start FlightEvents.DiscordBot