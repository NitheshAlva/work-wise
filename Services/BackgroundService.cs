namespace workwise.Services;
public class RecurringTaskBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringTaskBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run once per day
    private readonly TimeSpan _dueTime = TimeSpan.FromHours(2); // Run at 2 AM

    public RecurringTaskBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RecurringTaskBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurring Task Background Service started");

        // Calculate initial delay to run at 2 AM
        var now = DateTime.Now;
        var nextRun = now.Date.Add(_dueTime);
        if (now > nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        var initialDelay = nextRun - now;
        _logger.LogInformation("Next recurring task generation scheduled for: {NextRun}", nextRun);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(_period);

        do
        {
            try
            {
                await GenerateRecurringTasks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating recurring tasks");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GenerateRecurringTasks()
    {
        _logger.LogInformation("Starting recurring task generation at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var recurringTaskService = scope.ServiceProvider.GetRequiredService<IRecurringTaskService>();

        await recurringTaskService.GenerateTasksForAllActiveTemplatesAsync();

        _logger.LogInformation("Completed recurring task generation at {Time}", DateTime.UtcNow);
    }
}

