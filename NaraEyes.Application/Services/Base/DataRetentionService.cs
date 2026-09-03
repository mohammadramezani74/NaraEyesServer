using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Abstraction.Unitofwork;

namespace NaraEyes.Application.Services.Base
{
    /// <summary>
    /// پاکسازی تدریجی جدول‌های پرحجم.
    ///
    /// چرا لازم است: InBox و OutBox فقط Processed علامت می‌خورند و هرگز
    /// فیزیکی حذف نمی‌شوند. با ۳۰۰ دستگاه که هر ۳۵ ثانیه poll می‌زنند،
    /// روزانه صدها هزار ردیف اضافه می‌شود. کوئری‌ها به‌تدریج کند می‌شوند
    /// بدون اینکه علتش واضح باشد.
    ///
    /// طراحی عمداً محافظه‌کارانه است:
    ///   • حالت گزارش‌محور برای اطمینان قبل از اولین حذف واقعی
    ///   • حذف دسته‌ای تا قفل طولانی روی جدول نسازد
    ///   • مکث بین دسته‌ها
    ///   • فقط در پنجره‌ی کم‌ترافیک
    ///   • سقف حذف در هر اجرا
    /// </summary>
    public sealed class DataRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IConfiguration _config;

        private DateTime _lastRunDate = DateTime.MinValue;

        // ---- تنظیمات، از appsettings با مقدار پیش‌فرض امن ----
        private bool Enabled => _config.GetValue("Retention:Enabled", true);
        private bool DryRun => _config.GetValue("Retention:DryRun", true);

        private int InBoxDays => _config.GetValue("Retention:InBoxProcessedDays", 30);
        private int OutBoxDays => _config.GetValue("Retention:OutBoxProcessedDays", 30);
        private int EventDays => _config.GetValue("Retention:DeviceEventAckDays", 90);
        private int FaultDays => _config.GetValue("Retention:ModuleFaultResolvedDays", 365);

        private int BatchSize => _config.GetValue("Retention:BatchSize", 2000);
        private int BatchDelayMs => _config.GetValue("Retention:DelayBetweenBatchesMs", 500);
        private int MaxRowsPerRun => _config.GetValue("Retention:MaxRowsPerRun", 200_000);

        private int WindowStart => _config.GetValue("Retention:WindowStartHour", 2);
        private int WindowEnd => _config.GetValue("Retention:WindowEndHour", 5);
        private int CheckMinutes => _config.GetValue("Retention:CheckIntervalMinutes", 30);

        public DataRetentionService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataRetentionService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            if (!Enabled)
            {
                _logger.LogInformation("سرویس پاکسازی داده غیرفعال است.");
                return;
            }

            _logger.LogInformation(
                "سرویس پاکسازی فعال شد | حالت: {Mode} | پنجره: {Start}:00 تا {End}:00",
                DryRun ? "گزارش‌محور (بدون حذف)" : "حذف واقعی",
                WindowStart, WindowEnd);

            // مکث اولیه تا برنامه کامل بالا بیاید
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); }
            catch (OperationCanceledException) { return; }

            using var timer = new PeriodicTimer(
                TimeSpan.FromMinutes(Math.Max(5, CheckMinutes)));

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (ShouldRunNow())
                    {
                        await RunCleanupAsync(ct);
                        _lastRunDate = DateTime.Now.Date;
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "پاکسازی داده با خطا مواجه شد");
                }

                try { await timer.WaitForNextTickAsync(ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>در پنجره‌ی مجاز هستیم و امروز هنوز اجرا نشده؟</summary>
        private bool ShouldRunNow()
        {
            var now = DateTime.Now;

            if (_lastRunDate == now.Date) return false;

            int h = now.Hour;

            // پنجره ممکن است از نیمه‌شب رد شود (مثلاً ۲۳ تا ۳)
            return WindowStart <= WindowEnd
                ? h >= WindowStart && h < WindowEnd
                : h >= WindowStart || h < WindowEnd;
        }

        private async Task RunCleanupAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("═══ شروع پاکسازی داده ═══");

            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();

            long total = 0;

            var inboxCutoff = DateTime.Now.AddDays(-InBoxDays);
            var outboxCutoff = DateTime.Now.AddDays(-OutBoxDays);
            var eventCutoff = DateTime.Now.AddDays(-EventDays);
            var faultCutoff = DateTime.Now.AddDays(-FaultDays);

            total += await PurgeAsync("InBoxDeviceMessages", InBoxDays,
                () => uow.InBoxDeviceMessages
                         .Where(x => x.Processed && x.CreateDate < inboxCutoff),
                ct);

            total += await PurgeAsync("OutBoxDeviceMessages", OutBoxDays,
                () => uow.OutBoxDeviceMessages
                         .Where(x => (x.Processed || x.Deleted) && x.CreateDate < outboxCutoff),
                ct);

            total += await PurgeAsync("DeviceEvents", EventDays,
                () => uow.DeviceEvents
                         .Where(x => x.Acknowledged && x.EventTime < eventCutoff),
                ct);

            total += await PurgeAsync("ModuleFaultLogs", FaultDays,
                () => uow.ModuleFaultLogs
                         .Where(x => x.ResolvedAt != null && x.ResolvedAt < faultCutoff),
                ct);

            sw.Stop();

            _logger.LogInformation(
                "═══ پایان پاکسازی | {Total:N0} ردیف | {Sec} ثانیه | {Mode} ═══",
                total, sw.Elapsed.TotalSeconds.ToString("0.0"),
                DryRun ? "فقط گزارش" : "حذف شد");
        }

        /// <summary>
        /// حذف دسته‌ای یک جدول.
        ///
        /// ExecuteDeleteAsync مستقیم در دیتابیس حذف می‌کند بدون بارگذاری
        /// entity در حافظه — برای جدول‌های بزرگ حیاتی است.
        /// </summary>
        private async Task<long> PurgeAsync<T>(
            string tableName,
            int retentionDays,
            Func<IQueryable<T>> queryFactory,
            CancellationToken ct) where T : class
        {
            try
            {
                // ---- حالت گزارش‌محور ----
                if (DryRun)
                {
                    int count = await queryFactory().CountAsync(ct);

                    _logger.LogInformation(
                        "[گزارش] {Table}: {Count:N0} ردیف قدیمی‌تر از {Days} روز — حذف نشد",
                        tableName, count, retentionDays);

                    return count;
                }

                // ---- حذف واقعی، دسته‌دسته ----
                long deleted = 0;
                int batchNo = 0;

                while (!ct.IsCancellationRequested && deleted < MaxRowsPerRun)
                {
                    int affected = await EntityFrameworkQueryableExtensions
      .ExecuteDeleteAsync(queryFactory().Take(BatchSize), ct);

                    if (affected == 0) break;

                    deleted += affected;
                    batchNo++;

                    if (batchNo % 10 == 0)
                        _logger.LogInformation("{Table}: {Deleted:N0} ردیف…", tableName, deleted);

                    // به دیتابیس نفس بده
                    try { await Task.Delay(BatchDelayMs, ct); }
                    catch (OperationCanceledException) { break; }
                }

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "{Table}: مجموع {Deleted:N0} ردیف حذف شد (قدیمی‌تر از {Days} روز)",
                        tableName, deleted, retentionDays);
                }

                if (deleted >= MaxRowsPerRun)
                {
                    _logger.LogWarning(
                        "{Table}: به سقف {Max:N0} رسید — ادامه در اجرای بعدی",
                        tableName, MaxRowsPerRun);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "پاکسازی {Table} ناموفق بود", tableName);
                return 0;
            }
        }
    }
}