using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Base;

namespace NaraEyes.Domain.Entities.Devices
{
    /// <summary>
    /// یک بازه‌ی اجرای پیوسته‌ی سرور.
    ///
    /// چرا لازم است: بازه‌های DeviceStateLog وقتی سرور خاموش می‌شود باز
    /// می‌مانند. اگر سرور را برای یک استقرار ۱۰ دقیقه پایین بیاوری، آن ۱۰
    /// دقیقه به آخرین وضعیت هر ۳۰۰ دستگاه اضافه می‌شود — و چون معمولاً
    /// دستگاه‌ها در لحظه‌ی استقرار در وضعیت‌های مختلفی هستند، بخشی از آن
    /// به‌عنوان «خارج از سرویس» ثبت می‌شود.
    ///
    /// یعنی **هر استقرار، آمار آماده‌به‌کاری را خراب می‌کند** و هیچ‌کس هم
    /// متوجه نمی‌شود چون عدد فقط کمی پایین‌تر می‌آید.
    ///
    /// این جدول یک ردیف به‌ازای هر بار بالا آمدن سرور دارد. فاصله‌ی بین
    /// LastAliveAt یک ردیف و StartedAt ردیف بعدی، یعنی مدتی که سرور
    /// نمی‌دانسته چه خبر است — و آن مدت از مخرج کسر آماده‌به‌کاری کم
    /// می‌شود، نه اینکه به خارج از سرویس اضافه شود.
    ///
    /// اگر این جدول خالی باشد، گزارش دقیقاً مثل قبل کار می‌کند. یعنی
    /// می‌شود بعداً فعالش کرد بدون اینکه چیزی بشکند.
    /// </summary>
    public class ServerUptimeLog : BaseEntity
    {
        /// <summary>لحظه‌ی بالا آمدن سرور</summary>
        public DateTime StartedAt { get; private set; }

        /// <summary>آخرین ضربان — هر دقیقه به‌روز می‌شود</summary>
        public DateTime LastAliveAt { get; private set; }

        /// <summary>نسخه‌ی برنامه — برای اینکه بشود قطعی‌ها را به استقرارها ربط داد</summary>
        public string? Version { get; private set; }

        private ServerUptimeLog() { }

        public static ServerUptimeLog Start(DateTime at, string? version)
            => new()
            {
                Id = Guid.NewGuid(),
                StartedAt = at,
                LastAliveAt = at,
                Version = version,
                CreateDate = at,
            };

        public void Beat(DateTime at)
        {
            if (at > LastAliveAt) LastAliveAt = at;
            ModifiedDate = at;
        }
    }
}