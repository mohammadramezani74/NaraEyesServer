using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Domain.Entities.Devices;
using System.Reflection;

namespace NaraEyes.Application.Services.Reports
{
    /// <summary>
    /// یک ردیف به‌ازای هر بار بالا آمدن سرور می‌نویسد و هر دقیقه ضربانش را
    /// به‌روز می‌کند.
    ///
    /// چرا: بازه‌های DeviceStateLog وقتی سرور خاموش می‌شود باز می‌مانند.
    /// بدون این، هر استقرار ده‌دقیقه‌ای به آخرین وضعیت هر ۳۰۰ دستگاه اضافه
    /// می‌شود و درصد آماده‌به‌کاری را بی‌سروصدا پایین می‌آورد. کسی هم شک
    /// نمی‌کند چون عدد فقط «کمی» بدتر است.
    ///
    /// هزینه‌اش یک UPDATE در دقیقه است — در برابر ~۲۹ درخواست بر ثانیه‌ی
    /// موجود، عملاً صفر.
    /// </summary>
    public sealed class ServerUptimeTracker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ServerUptimeTracker> _logger;

        private static readonly TimeSpan BeatInterval = TimeSpan.FromMinutes(1);

        private Guid _runId;

        public ServerUptimeTracker(
            IServiceScopeFactory scopeFactory,
            ILogger<ServerUptimeTracker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // کمی صبر تا مهاجرت‌ها و راه‌اندازی اولیه تمام شود
            try { await Task.Delay(TimeSpan.FromSeconds(20), ct); }
            catch (OperationCanceledException) { return; }

            await StartRunAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(BeatInterval, ct);
                    await BeatAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // یک ضربان از دست رفته مسئله‌ی مهمی نیست — فقط نباید
                    // سرویس بمیرد، وگرنه از آن لحظه به بعد کل بازه به‌عنوان
                    // قطعی سرور حساب می‌شود و گزارش برعکس خراب می‌شود.
                    _logger.LogWarning(ex, "ServerUptimeTracker beat failed");
                }
            }
        }

        private async Task StartRunAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();

                var version = Assembly.GetEntryAssembly()?
                    .GetName().Version?.ToString();

                var run = ServerUptimeLog.Start(DateTime.Now, version);
                uow.ServerUptimeLogs.Add(run);
                await uow.SaveChangesAsync(ct);

                _runId = run.Id;
                _logger.LogInformation("ServerUptimeTracker started run {RunId}", _runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ServerUptimeTracker could not start a run");
            }
        }

        private async Task BeatAsync(CancellationToken ct)
        {
            if (_runId == Guid.Empty)
            {
                await StartRunAsync(ct);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();

            var run = await uow.ServerUptimeLogs.FindAsync(new object[] { _runId }, ct);
            if (run is null)
            {
                _runId = Guid.Empty;
                return;
            }

            run.Beat(DateTime.Now);
            await uow.SaveChangesAsync(ct);
        }
    }
}