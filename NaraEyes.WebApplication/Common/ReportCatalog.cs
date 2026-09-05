using MudBlazor;

namespace NaraEyes.WebApplication.Common
{
    /// <summary>
    /// تعریف یک گزارش در مرکز گزارش‌ها.
    /// </summary>
    /// <param name="Key">شناسه‌ی یکتا — در URL و لاگ استفاده می‌شود</param>
    /// <param name="TitleFa">عنوان نمایشی</param>
    /// <param name="DescriptionFa">توضیح کوتاه در کارت</param>
    /// <param name="Icon">آیکون MudBlazor</param>
    /// <param name="Route">مسیر صفحه</param>
    /// <param name="Category">دسته‌بندی برای گروه‌بندی در فهرست</param>
    /// <param name="Roles">نقش‌هایی که به این گزارش دسترسی دارند</param>
    /// <param name="IsReady">اگر false باشد، کارت به‌صورت «به‌زودی» نمایش داده می‌شود</param>
    public sealed record ReportDefinition(
        string Key,
        string TitleFa,
        string DescriptionFa,
        string Icon,
        string Route,
        string Category,
        string[] Roles,
        bool IsReady = true);

    /// <summary>
    /// فهرست گزارش‌های تحلیلی.
    ///
    /// اینجا فقط گزارش‌هایی می‌آیند که روی مجموعه‌ای از دستگاه‌ها تحلیل
    /// می‌کنند. عملیات تک‌دستگاهی (دریافت تصاویر، لاگ، اسکرین‌شات) در
    /// منوی اکشن‌های صفحه‌ی مدیریت دستگاه‌ها قرار می‌گیرند، نه اینجا.
    ///
    /// افزودن گزارش جدید = یک ردیف اینجا + یک فایل razor.
    /// </summary>
    public static class ReportCatalog
    {
        // ---- دسته‌بندی‌ها ----
        public const string CatHardware = "سخت‌افزار";
        public const string CatOperations = "عملیات";
        public const string CatFinancial = "نقدینگی";

        // ---- نقش‌ها (مطابق نقش‌های موجود سیستم) ----
        private const string RoleCentral = "مدیریت مرکزی";
        private const string RoleMonitoring = "مدیریت مانیتورینگ";
        private const string RoleProvince = "مدیریت استان";
        private const string RoleSecurity = "حراست مرکزی";

        public static readonly IReadOnlyList<ReportDefinition> All = new List<ReportDefinition>
        {
            // ================= سخت‌افزار =================

            // درخواست ۲
            new(
                Key:           "module-faults",
                TitleFa:       "خرابی قطعات",
                DescriptionFa: "تعداد و مدت خرابی هر ماژول سخت‌افزاری، به تفکیک دستگاه و بازه‌ی زمانی",
                Icon:          Icons.Material.Filled.BuildCircle,
                Route:         "/reports/module-faults",
                Category:      CatHardware,
                Roles:         new[] { RoleCentral, RoleMonitoring },
                IsReady:       true),

            // درخواست ۷
            new(
                Key:           "hardware-changes",
                TitleFa:       "تغییرات سخت‌افزاری",
                DescriptionFa: "شناسایی تعویض پردازنده، حافظه، دیسک یا مادربرد روی دستگاه‌ها",
                Icon:          Icons.Material.Filled.Memory,
                Route:         "/reports/hardware-changes",
                Category:      CatHardware,
                Roles:         new[] { RoleCentral, RoleSecurity },
                IsReady:       false),

            // ================= عملیات =================

            // درخواست ۶
            new(
                Key:           "device-availability",
                TitleFa:       "آماده‌به‌کاری دستگاه‌ها",
                DescriptionFa: "مدت زمان در سرویس، خارج از سرویس و خطا برای هر دستگاه",
                Icon:          Icons.Material.Filled.Timeline,
                Route:         "/reports/device-availability",
                Category:      CatOperations,
                Roles:         new[] { RoleCentral, RoleMonitoring, RoleProvince },
                IsReady:       true),

            // ================= نقدینگی =================

            // درخواست ۳
            new(
                Key:           "cash-inventory",
                TitleFa:       "موجودی کاست‌ها",
                DescriptionFa: "موجودی فعلی و ارزش ریالی کاست‌ها، به تفکیک شعبه و سرپرستی",
                Icon:          Icons.Material.Filled.AccountBalanceWallet,
                Route:         "/reports/cash-inventory",
                Category:      CatFinancial,
                Roles:         new[] { RoleCentral, RoleProvince },
                IsReady:       true),
        };

        /// <summary>ترتیب نمایش دسته‌ها در صفحه‌ی فهرست</summary>
        public static readonly string[] CategoryOrder =
        {
            CatHardware,
            CatOperations,
            CatFinancial,
        };
    }
}