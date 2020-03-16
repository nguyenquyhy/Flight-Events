#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-nanoserver-1903 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build
WORKDIR /src
COPY ["FlightEvents.DiscordBot/FlightEvents.DiscordBot.csproj", "FlightEvents.DiscordBot/"]
RUN dotnet restore "FlightEvents.DiscordBot/FlightEvents.DiscordBot.csproj"
COPY . .
WORKDIR "/src/FlightEvents.DiscordBot"
RUN dotnet build "FlightEvents.DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightEvents.DiscordBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlightEvents.DiscordBot.dll"]