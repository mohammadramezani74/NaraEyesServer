using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Contracts.Interfaces.Devices;

namespace NaraEyes.Application.Services.Base;

/// <summary>
/// دستگاه‌هایی که مدتی ضربان نفرستاده‌اند را آفلاین می‌کند.
///
/// قبلاً این کار در OnInitializedAsync صفحه انجام می‌شد که سه ایراد داشت:
///   ۱) با هر کاربر و هر بار باز کردن صفحه تکرار می‌شد
///   ۲) روی همان DbContext صفحه کار می‌کرد → تداخل و قفل شدن جدول
///   ۳) اگر کسی صفحه را باز نمی‌کرد، اصلاً اجرا نمی‌شد
/// </summary>
public sealed class HeartbeatMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HeartbeatMonitor> _logger;

    public HeartbeatMonitor(IServiceScopeFactory scopeFactory, ILogger<HeartbeatMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // کمی صبر تا برنامه کامل بالا بیاید
        try { await Task.Delay(TimeSpan.FromSeconds(20), ct); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IDeviceService>();

                int n = await svc.CheckHeartBeat(ct);

                if (n > 0)
                    _logger.LogInformation("{Count} دستگاه به دلیل نبود ضربان آفلاین شد.", n);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HeartbeatMonitor failed");
            }

            try { await timer.WaitForNextTickAsync(ct); }
            catch (OperationCanceledException) { break; }
        }
    }
}