﻿using Discord;
using Discord.Rest;
using FlightEvents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlightEvents.Web.Logics
{
    public class DiscordOptions
    {
        [Required]
        public string ClientId { get; set; }
        [Required]
        public string ClientSecret { get; set; }
        [Required]
        public string RedirectUri { get; set; }
        [Required]
        public ulong ServerId { get; set; }
        [Required]
        public bool AddUserToServer { get; set; }
        [Required]
        public string BotToken { get; set; }
    }

    public class DiscordLoginResult
    {
        public DiscordLoginResult(IUser user, string confirmCode)
        {
            User = user;
            ConfirmCode = confirmCode;
        }

        public IUser User { get; }
        public string ConfirmCode { get; }
    }

    public class DiscordLogic
    {
        private readonly ILogger<DiscordLogic> logger;
        private readonly HttpClient httpClient;
        private readonly DiscordOptions options;

        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private static readonly Random random = new Random();

        private static readonly ConcurrentDictionary<string, (DateTimeOffset, RestSelfUser, Tokens)> pendingConnections = new ConcurrentDictionary<string, (DateTimeOffset, RestSelfUser, Tokens)>();

        public DiscordLogic(ILogger<DiscordLogic> logger, HttpClient httpClient, IOptionsMonitor<DiscordOptions> optionsAccessor, IDiscordConnectionStorage discordConnectionStorage)
        {
            this.logger = logger;
            this.httpClient = httpClient;
            this.options = optionsAccessor.CurrentValue;
            this.discordConnectionStorage = discordConnectionStorage;
        }

        public async Task<DiscordLoginResult> LoginAsync(string authCode)
        {
            var response = await httpClient.PostAsync("https://discordapp.com/api/oauth2/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["redirect_uri"] = options.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["scope"] = "identify guilds.join",
                ["code"] = authCode
            }));
            using var stream = await response.Content.ReadAsStreamAsync();
            var tokens = await JsonSerializer.DeserializeAsync<Tokens>(stream);

            var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bearer, tokens.access_token);
            var confirmCode = GenerateCode();

            pendingConnections.TryAdd(confirmCode, (DateTimeOffset.Now, client.CurrentUser, tokens));

            return new DiscordLoginResult(client.CurrentUser, confirmCode);
        }

        public async Task<DiscordConnection> ConfirmAsync(string clientId, string code)
        {
            if (pendingConnections.TryGetValue(code, out var value))
            {
                var user = value.Item2;
                var tokens = value.Item3;

                var connection = await discordConnectionStorage.StoreConnectionAsync(clientId, user.Id, user.Username, user.Discriminator);

                var discordClient = new DiscordRestClient();
                await discordClient.LoginAsync(TokenType.Bearer, tokens.access_token);

                ulong guildId = options.ServerId;

                if (options.AddUserToServer)
                {
                    try
                    {
                        var botClient = new DiscordRestClient();
                        await botClient.LoginAsync(TokenType.Bot, options.BotToken);
                        var guild = await botClient.GetGuildAsync(guildId);
                        await guild.AddGuildUserAsync(discordClient.CurrentUser.Id, tokens.access_token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Cannot add user {userId} {username}#{userDiscriminator} to server {guildId}!",
                            user.Id, user.Username, user.Discriminator, guildId);
                    }
                }

                return connection;
            }

            return null;
        }

        public Task<DiscordConnection> GetConnectionAsync(string clientId)
            => discordConnectionStorage.GetConnectionAsync(clientId);

        public Task DeleteConnectionAsync(string clientId)
            => discordConnectionStorage.DeleteConnectionAsync(clientId);

        private string GenerateCode()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < 6; i++)
            {
                builder.Append((char)('A' + (char)random.Next(26)));
            }
            return builder.ToString();
        }
    }

    public class Tokens
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }
}
