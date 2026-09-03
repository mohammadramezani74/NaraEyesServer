using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    /// <summary>
    /// یک بازه‌ی خرابی برای یک ماژول سخت‌افزاری.
    ///
    /// طراحی عمداً «بازه‌محور» است نه «رویدادمحور»: هنگام ورود ماژول به
    /// وضعیت خطا یک ردیف باز می‌شود و هنگام بازگشت به حالت عادی بسته
    /// می‌شود. بنابراین دستگاهی که یک ماه خراب بماند فقط یک ردیف دارد،
    /// نه هزاران ردیف به‌ازای هر چرخه‌ی متریک.
    ///
    /// اگر نوع خطا در میانه عوض شود (مثلاً HardwareError → Offline) همان
    /// بازه باز می‌ماند و فقط وضعیت به‌روز می‌شود، چون از دید عملیاتی
    /// دستگاه در تمام آن مدت از کار افتاده بوده است.
    /// </summary>
    public class ModuleFaultLog : BaseEntity
    {
        public Guid DeviceId { get; private set; }
        public Device Device { get; private set; } = null!;

        public Guid? DeviceModuleId { get; private set; }

        public DeviceModuleType Module { get; private set; }

        /// <summary>وضعیتی که بازه با آن شروع شد</summary>
        public HealthStatus StartStatus { get; private set; }

        /// <summary>آخرین وضعیت خطا در این بازه — ممکن است با StartStatus فرق کند</summary>
        public HealthStatus CurrentStatus { get; private set; }

        /// <summary>مقدار خام fwDevice در لحظه‌ی ثبت</summary>
        public ushort RawStatus { get; private set; }

        /// <summary>توضیح فارسی، مثلاً «کاغذ تمام شده»</summary>
        public string? Detail { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? ResolvedAt { get; private set; }

        /// <summary>مدت خرابی به ثانیه — هنگام بسته شدن پر می‌شود</summary>
        public int? DurationSeconds { get; private set; }

        /// <summary>تعداد دفعاتی که نوع خطا در این بازه عوض شده</summary>
        public int TransitionCount { get; private set; }

        public bool IsOpen => ResolvedAt is null;

        private ModuleFaultLog() { }

        public static ModuleFaultLog Open(
            Guid deviceId,
            Guid? moduleId,
            DeviceModuleType module,
            HealthStatus status,
            ushort raw,
            string? detail,
            DateTime at)
            => new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                DeviceModuleId = moduleId,
                Module = module,
                StartStatus = status,
                CurrentStatus = status,
                RawStatus = raw,
                Detail = detail,
                StartedAt = at,
                TransitionCount = 0,
                CreateDate = at,
            };

        /// <summary>نوع خطا عوض شده ولی ماژول همچنان خراب است</summary>
        public void Transition(HealthStatus status, ushort raw, string? detail, DateTime at)
        {
            if (ResolvedAt is not null) return;
            if (CurrentStatus == status) return;

            CurrentStatus = status;
            RawStatus = raw;
            if (!string.IsNullOrWhiteSpace(detail)) Detail = detail;

            TransitionCount++;
            ModifiedDate = at;
        }

        /// <summary>ماژول به حالت عادی برگشت</summary>
        public void Resolve(DateTime at)
        {
            if (ResolvedAt is not null) return;

            ResolvedAt = at;
            DurationSeconds = Math.Max(0, (int)(at - StartedAt).TotalSeconds);
            ModifiedDate = at;
        }

        /// <summary>مدت خرابی — برای بازه‌های باز تا لحظه‌ی درخواست محاسبه می‌شود</summary>
        public int GetDurationSeconds(DateTime asOf)
            => DurationSeconds ?? Math.Max(0, (int)(asOf - StartedAt).TotalSeconds);
    }
}