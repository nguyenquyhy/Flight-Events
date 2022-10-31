using Microsoft.Extensions.Logging;

namespace FlightEvents.Bots.Logics;

public static class TaskExtensions
{
    public static void RunInThreadPool(this Task task, ILogger logger)
    {
        Task.Run(async () =>
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing in thread pool.");
                throw;
            }
        });
    }
}