using Engine.Worker.Jobs;

namespace Engine.Worker;

/// <summary>백그라운드 Job 스케쥴러 - 단순 타이머 기반</summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    private static readonly TimeSpan ApprovalReminderInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan NotificationCleanupInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan AttendanceAutoCloseInterval = TimeSpan.FromHours(1);

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Service 시작");

        await Task.WhenAll(
            RunLoopAsync("ApprovalReminder", ApprovalReminderInterval,
                async (scope, ct) =>
                {
                    var job = scope.ServiceProvider.GetRequiredService<ApprovalReminderJob>();
                    await job.ExecuteAsync(ct);
                }, stoppingToken),

            RunLoopAsync("NotificationCleanup", NotificationCleanupInterval,
                async (scope, ct) =>
                {
                    if (DateTime.Now.Hour >= 3)
                    {
                        var job = scope.ServiceProvider.GetRequiredService<NotificationCleanupJob>();
                        await job.ExecuteAsync(ct);
                    }
                }, stoppingToken),

            RunLoopAsync("AttendanceAutoClose", AttendanceAutoCloseInterval,
                async (scope, ct) =>
                {
                    if (DateTime.Now.Hour == 0)
                    {
                        var job = scope.ServiceProvider.GetRequiredService<AttendanceAutoCloseJob>();
                        await job.ExecuteAsync(ct);
                    }
                }, stoppingToken)
        );
    }

    private async Task RunLoopAsync(
        string jobName,
        TimeSpan interval,
        Func<IServiceScope, CancellationToken, Task> action,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                await action(scope, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{JobName}] 실행 중 오류 발생", jobName);
            }

            await Task.Delay(interval, ct).ContinueWith(_ => { }, CancellationToken.None);
        }
    }
}
