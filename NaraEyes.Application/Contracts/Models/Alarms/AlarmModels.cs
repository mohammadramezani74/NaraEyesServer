using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Alarms
{
    /// <summary>
    /// کدهای آلارم — یکدست بودنشان مهم است چون فیلتر، گزارش و پاکسازی
    /// همه رویشان بنا می‌شوند. رشته‌ی آزاد ننویس.
    /// </summary>
    public static class AlarmCodes
    {
        // --- سخت‌افزار (درخواست ۷) ---
        public const string HardwareChanged = "HW-CHANGED";
        public const string HardwareDowngrade = "HW-DOWNGRADE";

        /// <summary>
        /// اولین ثبت پروفایل. عمداً Info است و آلارم حساب نمی‌شود —
        /// وگرنه روز اول ۳۰۰ آلارم می‌گیری و کاربر از همان اول یاد
        /// می‌گیرد زنگوله را نادیده بگیرد.
        /// </summary>
        public const string HardwareBaseline = "HW-BASELINE";

        // --- ماژول‌ها (درخواست ۲) ---
        public const string ModuleFault = "MODULE-FAULT";
        public const string ModuleRecovered = "MODULE-OK";

        // --- وضعیت (درخواست ۶) ---
        public const string DeviceOffline = "DEVICE-OFFLINE";
        public const string DeviceOutOfService = "DEVICE-OOS";

        // --- نقدینگی (درخواست ۳) ---
        public const string CashLow = "CASH-LOW";

        public static string Fa(string code)
        {
            if (code == HardwareChanged) return "تغییر سخت‌افزار";
            if (code == HardwareDowngrade) return "تنزل سخت‌افزار";
            if (code == HardwareBaseline) return "ثبت پروفایل اولیه";
            if (code == ModuleFault) return "خرابی ماژول";
            if (code == ModuleRecovered) return "رفع خرابی";
            if (code == DeviceOffline) return "قطع ارتباط";
            if (code == DeviceOutOfService) return "خارج از سرویس";
            if (code == CashLow) return "کمبود موجودی";
            return code;
        }
    }

    /// <summary>
    /// آنچه از طریق SignalR پخش می‌شود.
    ///
    /// ⚠️ این پیام به **همه‌ی** کاربران متصل می‌رود، مثل
    /// ReceiveDeviceStatusUpdate که از قبل وجود دارد. اگر روزی تفکیک
    /// بر اساس استان لازم شد، باید هم اینجا و هم آنجا اعمال شود —
    /// محدود کردن فقط آلارم‌ها امنیت کاذب می‌سازد.
    /// </summary>
    public sealed class AlarmNotification
    {
        public Guid EventId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceIp { get; set; } = "";
        public string? BranchName { get; set; }
        public EventSeverity Severity { get; set; }
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime OccurredAt { get; set; }
    }

    public sealed class AlarmFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public EventSeverity? Severity { get; set; }
        public string? Code { get; set; }
        public Guid? DeviceId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? SupervisionId { get; set; }

        /// <summary>null = همه، false = فقط تأییدنشده‌ها</summary>
        public bool? Acknowledged { get; set; } = false;

        public string? Search { get; set; }

        public int Take { get; set; } = 200;
    }

    public sealed class AlarmRow
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceIp { get; set; } = "";
        public int? DeviceCode { get; set; }
        public string? BranchName { get; set; }
        public string? SupervisionName { get; set; }

        public EventSeverity Severity { get; set; }
        public DeviceModuleType Module { get; set; }
        public string Code { get; set; } = "";
        public string CodeFa { get; set; } = "";
        public string Message { get; set; } = "";
        public string? PayloadJson { get; set; }

        public DateTime EventTime { get; set; }

        public bool Acknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedByName { get; set; }

        public string SeverityFa => Severity switch
        {
            EventSeverity.Critical => "بحرانی",
            EventSeverity.Error => "خطا",
            EventSeverity.Warning => "هشدار",
            _ => "اطلاعی",
        };
    }

    public sealed class AlarmCounts
    {
        public int Unacknowledged { get; set; }
        public int Critical { get; set; }
        public int Error { get; set; }
        public int Warning { get; set; }
    }
}