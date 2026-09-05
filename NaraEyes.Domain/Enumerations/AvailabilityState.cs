using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Domain.Enumerations
{
    /// <summary>
    /// دسته‌بندی عملیاتی وضعیت دستگاه — آنچه در گزارش آماده‌به‌کاری شمرده می‌شود.
    ///
    /// این با DeviceMode یکی نیست و عمداً هم نباید باشد. DeviceMode هشت
    /// مقدار دارد که ترکیبی از «وضعیت سرویس‌دهی» و «هشدار» است؛ اینجا فقط
    /// یک سؤال پرسیده می‌شود: در این بازه مشتری می‌توانست پول بگیرد یا نه.
    /// </summary>
    public enum AvailabilityState
    {
        /// <summary>
        /// دستگاه سرویس می‌داد. شامل هشدارهای کاغذ و موجودی هم می‌شود،
        /// چون دستگاه در آن حالت‌ها همچنان پول می‌دهد.
        /// </summary>
        [Display(Name = "آماده‌به‌کار")]
        Available = 1,

        /// <summary>
        /// خارج از سرویس به تصمیم اپراتور یا به‌خاطر پین‌پد — دستگاه سالم
        /// است ولی سرویس نمی‌دهد.
        /// </summary>
        [Display(Name = "خارج از سرویس")]
        OutOfService = 2,

        /// <summary>خطای سخت‌افزاری</summary>
        [Display(Name = "خطا")]
        Error = 3,

        /// <summary>
        /// کلید اپراتور روی RUN است ولی ارتباط با سوییچ سپنتا برقرار نیست.
        /// دستگاه سالم است ولی تراکنش انجام نمی‌شود، پس آماده‌به‌کار نیست.
        /// </summary>
        [Display(Name = "قطع ارتباط")]
        Disconnected = 4,

        /// <summary>وضعیت نامشخص — نه در مخرج کسر می‌آید نه در صورت</summary>
        [Display(Name = "نامشخص")]
        Unknown = 5,
    }

    /// <summary>
    /// نگاشت DeviceMode به AvailabilityState.
    ///
    /// عمداً در Domain گذاشته شده و نه در سرویس گزارش، چون هم ثبت‌کننده و
    /// هم گزارش‌گیر باید **دقیقاً** یک تعریف داشته باشند. اگر دو جا کپی
    /// شود، روزی یکی عوض می‌شود و آن یکی نه، و اختلافش تا مدت‌ها دیده
    /// نمی‌شود.
    /// </summary>
    public static class AvailabilityMapping
    {
        public static AvailabilityState FromMode(DeviceMode mode)
        {
            // ⚠️ هشدارها آماده‌به‌کار حساب می‌شوند.
            //
            // در ایجنت (XFSFunctionality.GetCassetInfo) ترتیب شرط‌ها این است
            // که warning_paper و warning_Money **قبل از** InService بررسی
            // می‌شوند. یعنی دستگاهی که کاملاً سالم است، به‌محض کم شدن کاغذ
            // یا رسیدن موجودی به زیر ۲۰ میلیون، دیگر InService گزارش
            // نمی‌شود.
            //
            // اگر این‌ها را «خارج از سرویس» بشماریم، در پایان هر روز کاری
            // که موجودی طبیعتاً پایین می‌آید کل ناوگان خارج از سرویس
            // می‌شود و عدد گزارش بی‌معنی خواهد بود.
            if (mode == DeviceMode.InService) return AvailabilityState.Available;
            if (mode == DeviceMode.warning) return AvailabilityState.Available;
            if (mode == DeviceMode.warning_paper) return AvailabilityState.Available;
            if (mode == DeviceMode.warning_Money) return AvailabilityState.Available;

            if (mode == DeviceMode.Error) return AvailabilityState.Error;

            // Offline در ایجنت یعنی «کلید اپراتور RUN است ولی پینگ به سوییچ
            // سپنتا برقرار نیست» — دستگاه آماده است ولی تراکنش ممکن نیست.
            if (mode == DeviceMode.Offline) return AvailabilityState.Disconnected;

            // Online یعنی «پینگ هست ولی کلید اپراتور RUN نیست» — یعنی
            // اپراتور خودش دستگاه را خارج کرده.
            if (mode == DeviceMode.Online) return AvailabilityState.OutOfService;
            if (mode == DeviceMode.Supervisor) return AvailabilityState.OutOfService;

            return AvailabilityState.Unknown;
        }

        public static string Fa(AvailabilityState s)
        {
            if (s == AvailabilityState.Available) return "آماده‌به‌کار";
            if (s == AvailabilityState.OutOfService) return "خارج از سرویس";
            if (s == AvailabilityState.Error) return "خطا";
            if (s == AvailabilityState.Disconnected) return "قطع ارتباط";
            return "نامشخص";
        }

        public static string ModeFa(DeviceMode m)
        {
            if (m == DeviceMode.InService) return "در حال سرویس‌دهی";
            if (m == DeviceMode.Supervisor) return "خارج از سرویس";
            if (m == DeviceMode.warning) return "هشدار";
            if (m == DeviceMode.Error) return "خطا";
            if (m == DeviceMode.Offline) return "آفلاین";
            if (m == DeviceMode.Online) return "آنلاین";
            if (m == DeviceMode.warning_paper) return "هشدار کمبود کاغذ";
            if (m == DeviceMode.warning_Money) return "هشدار کمبود پول";
            return "نامشخص";
        }
    }
}