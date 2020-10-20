using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class EventOptions
    {
        [Required]
        public string FilePath { get; set; }
    }

    public class JsonFileFlightEventStorage : IFlightEventStorage
    {
        private readonly string filePath;
        private readonly RandomStringGenerator randomStringGenerator;

        public JsonFileFlightEventStorage(IOptionsMonitor<EventOptions> optionsAccessor, RandomStringGenerator randomStringGenerator)
        {
            this.filePath = optionsAccessor.CurrentValue.FilePath;
            this.randomStringGenerator = randomStringGenerator;
        }

        public async Task<IEnumerable<FlightEvent>> GetAllAsync() => await LoadAsync();

        public async Task<FlightEvent> GetAsync(Guid id) => (await LoadAsync()).FirstOrDefault(o => o.Id == id);

        public async Task<FlightEvent> GetByCodeAsync(string code) => (await LoadAsync()).FirstOrDefault(o => o.Code == code);

        public async Task<FlightEvent> AddAsync(FlightEvent flightEvent)
        {
            flightEvent.Id = Guid.NewGuid();
            flightEvent.CreatedDateTime = flightEvent.UpdatedDateTime = DateTimeOffset.UtcNow;
            flightEvent.Code = randomStringGenerator.Generate(8);

            var events = await LoadAsync();
            events.Add(flightEvent);
            await SaveAsync(events);
            return flightEvent;
        }

        public async Task<FlightEvent> UpdateAsync(FlightEvent flightEvent)
        {
            flightEvent.UpdatedDateTime = DateTimeOffset.UtcNow;

            var events = await LoadAsync();
            events.RemoveAll(o => o.Id == flightEvent.Id);
            events.Add(flightEvent);
            await SaveAsync(events);
            return flightEvent;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var events = await LoadAsync();
            var result = events.RemoveAll(o => o.Id == id);
            await SaveAsync(events);
            return result > 0;
        }

        private readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        private List<FlightEvent> flightEvents = null;

        private async Task<List<FlightEvent>> LoadAsync()
        {
            try
            {
                await sm.WaitAsync();

                if (flightEvents == null)
                {
                    if (File.Exists(filePath))
                    {
                        using var file = File.OpenRead(filePath);
                        flightEvents = await JsonSerializer.DeserializeAsync<List<FlightEvent>>(file);
                    }
                    else
                    {
                        flightEvents = new List<FlightEvent>();
                    }
                }

                return flightEvents.ToList();
            }
            finally
            {
                sm.Release();
            }
        }

        private async Task SaveAsync(List<FlightEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));


            try
            {
                await sm.WaitAsync();

                flightEvents = events;

                if (File.Exists(filePath))
                {
                    File.Move(filePath, filePath + ".bak", true);
                }

                using var file = File.OpenWrite(filePath);
                await JsonSerializer.SerializeAsync(file, flightEvents);
            }
            finally
            {
                sm.Release();
            }
        }
    }
}
