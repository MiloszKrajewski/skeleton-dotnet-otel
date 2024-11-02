using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebApp.Host;

public class BackgroundLoop: BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("BackgroundLoop");
    private static readonly Meter Meter = new("BackgroundLoop");

    private static readonly Counter<long> Counter = Meter.CreateCounter<long>(
        "background_loop_counter", description: "Counts how many times background loop executed");

    protected readonly ILogger Log;

    public BackgroundLoop(ILogger<BackgroundLoop> logger) => Log = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(4900, stoppingToken);

            using var activity = ActivitySource.StartActivity(
                "Execution", ActivityKind.Internal, Activity.Current?.Context ?? default);
            await Task.Delay(100, stoppingToken);
            Log.LogInformation("Background loop retried {Time}", DateTime.UtcNow);
            Counter.Add(1);
        }
    }
}
