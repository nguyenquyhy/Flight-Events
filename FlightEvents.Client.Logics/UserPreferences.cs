﻿using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class UserPreferences
    {
        public string LastCallsign { get; set; }
        public string ClientId { get; set; }
        public bool DisableDiscordRP { get; set; }
        public bool BroadcastUDP { get; set; }
        public string BroadcastIP { get; set; }
        public bool SlowMode { get; set; }
        public bool MinimizeToTaskbar { get; set; }
        public bool ShowLandingInfo { get; set; }
    }

    public class UserPreferencesLoader
    {
        private const string filePath = "preferences.json";
        
        private readonly ILogger<UserPreferencesLoader> logger;

        public UserPreferencesLoader(ILogger<UserPreferencesLoader> logger)
        {
            this.logger = logger;
        }

        public async Task<T> GetSettingsAsync<T>(Func<UserPreferences, T> extractFunc, T defaultValue = default)
        {
            try
            {
                var pref = await LoadAsync();
                if (pref == null) return defaultValue;
                return extractFunc(pref);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot get settings");
                return defaultValue;
            }
        }

        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public async Task<UserPreferences> LoadAsync()
        {
            try
            {
                await sm.WaitAsync();

                return await LoadFromFileAsync();
            }
            finally
            {
                sm.Release();
            }
        }

        public async Task<UserPreferences> UpdateAsync(Action<UserPreferences> updateAction)
        {
            _ = updateAction ?? throw new ArgumentNullException(nameof(updateAction));

            try
            {
                await sm.WaitAsync();

                var pref = await LoadFromFileAsync();
                updateAction(pref);
                await SaveToFileAsync(pref);
                return pref;
            }
            finally
            {
                sm.Release();
            }
        }

        private async Task SaveToFileAsync(UserPreferences pref)
        {
            if (File.Exists(filePath)) File.Move(filePath, filePath + ".bak", true);
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, pref);
        }

        private async Task<UserPreferences> LoadFromFileAsync()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using var stream = File.OpenRead(filePath);
                    return await JsonSerializer.DeserializeAsync<UserPreferences>(stream) ?? new UserPreferences();
                }
                catch (JsonException)
                {
                    var pref = new UserPreferences
                    {
                        ClientId = Guid.NewGuid().ToString("N")
                    };
                    await SaveToFileAsync(pref);
                    return pref;
                }
            }
            else
            {
                var pref = new UserPreferences
                {
                    ClientId = Guid.NewGuid().ToString("N")
                };
                await SaveToFileAsync(pref);
                return pref;
            }
        }
    }
}
