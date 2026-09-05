using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    /// <summary>
    /// یک بازه‌ی پیوسته که دستگاه در یک وضعیت عملیاتی بوده.
    ///
    /// همان الگوی ModuleFaultLog: بازه‌محور، نه رویدادمحور. هنگام تغییر
    /// وضعیت، بازه‌ی قبلی بسته و بازه‌ی جدید باز می‌شود. دستگاهی که یک ماه
    /// آماده‌به‌کار بماند **یک ردیف** دارد، نه ۱۴٬۴۰۰ ردیف به‌ازای هر
    /// چرخه‌ی متریک.
    ///
    /// ⚠️ تفاوت مهم با ModuleFaultLog: اینجا **همه‌ی** وضعیت‌ها ثبت
    /// می‌شوند، نه فقط خرابی‌ها. دلیلش این است که درصد آماده‌به‌کاری یک
    /// کسر است و برای مخرجش باید بدانیم اصلاً چقدر مدت دستگاه را رصد
    /// کرده‌ایم. اگر فقط خرابی‌ها را ثبت کنیم، نمی‌شود بین «دستگاه سالم
    /// بود» و «دستگاه اصلاً گزارش نمی‌داد» تفاوت گذاشت.
    /// </summary>
    public class DeviceStateLog : BaseEntity
    {
        public Guid DeviceId { get; private set; }
        public Device Device { get; private set; } = null!;

        /// <summary>دسته‌ی عملیاتی — همین در گزارش شمرده می‌شود</summary>
        public AvailabilityState State { get; private set; }

        /// <summary>DeviceMode دقیقی که بازه با آن شروع شد</summary>
        public DeviceMode StartMode { get; private set; }

        /// <summary>
        /// آخرین DeviceMode در این بازه.
        ///
        /// می‌تواند با StartMode فرق کند بدون اینکه بازه بسته شود — مثلاً
        /// InService → warning_Money. هر دو در دسته‌ی Available هستند، پس
        /// از دید آماده‌به‌کاری چیزی عوض نشده و بازه باید پیوسته بماند.
        /// ولی جزئیاتش را نگه می‌داریم چون بعداً ممکن است لازم شود.
        /// </summary>
        public DeviceMode CurrentMode { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        /// <summary>مدت به ثانیه — هنگام بسته شدن پر می‌شود</summary>
        public int? DurationSeconds { get; private set; }

        /// <summary>
        /// آخرین لحظه‌ای که دستگاه در این وضعیت **دیده شده**.
        ///
        /// با هر چرخه‌ی متریک به‌روز می‌شود. تفاوتش با EndedAt این است که
        /// EndedAt یعنی «وضعیت عوض شد» ولی LastSeenAt یعنی «تا اینجا مطمئنیم».
        /// اگر دستگاه ناگهان قطع شود، بازه باز می‌ماند ولی LastSeenAt نشان
        /// می‌دهد داده‌ی واقعی تا کجا بوده.
        /// </summary>
        public DateTime LastSeenAt { get; private set; }

        /// <summary>تعداد دفعاتی که Mode در همین بازه عوض شده</summary>
        public int TransitionCount { get; private set; }

        public bool IsOpen => EndedAt is null;

        private DeviceStateLog() { }

        public static DeviceStateLog Open(
            Guid deviceId, AvailabilityState state, DeviceMode mode, DateTime at)
            => new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                State = state,
                StartMode = mode,
                CurrentMode = mode,
                StartedAt = at,
                LastSeenAt = at,
                TransitionCount = 0,
                CreateDate = at,
            };

        /// <summary>
        /// دستگاه هنوز در همین دسته است. اگر Mode دقیق عوض شده باشد ثبت
        /// می‌شود، ولی بازه بسته نمی‌شود.
        /// </summary>
        public void Touch(DeviceMode mode, DateTime at)
        {
            if (EndedAt is not null) return;

            LastSeenAt = at;

            if (CurrentMode != mode)
            {
                CurrentMode = mode;
                TransitionCount++;
            }

            ModifiedDate = at;
        }

        /// <summary>دسته عوض شد — این بازه تمام است</summary>
        public void Close(DateTime at)
        {
            if (EndedAt is not null) return;

            EndedAt = at;
            DurationSeconds = Math.Max(0, (int)(at - StartedAt).TotalSeconds);
            ModifiedDate = at;
        }

        /// <summary>مدت بازه — برای بازه‌های باز تا لحظه‌ی درخواست</summary>
        public int GetDurationSeconds(DateTime asOf)
            => DurationSeconds ?? Math.Max(0, (int)(asOf - StartedAt).TotalSeconds);
    }
}