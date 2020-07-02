using System;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public interface IUserPreferencesLoader
    {
        Task<UserPreferences> LoadAsync();
        Task<UserPreferences> UpdateAsync(Action<UserPreferences> updateAction);
    }
}