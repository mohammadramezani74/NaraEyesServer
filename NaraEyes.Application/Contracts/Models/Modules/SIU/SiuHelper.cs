using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.SIU
{
    public static class SiuHelper
    {
        private static readonly Dictionary<ushort, string> DeviceMap = new()
    {
        { 0, "آنلاین (DEVONLINE)" },
        { 1, "آفلاین (DEVOFFLINE)" },
        { 2, "خطای سخت‌افزاری (DEVHWERROR)" },
        { 3, "بدون پشتیبانی/ناموجود (DEVNODEVICE)" },
        { 4, "قطع ارتباط (DEVPOWEROFF/USERERROR/NOFIRMWARE)"} // بسته به Vendor
    };

        // Doors index → title
        private static readonly (string Key, string TitleFa)[] DoorSlots = new[]
        {
        ("Cabinet",       "درِ کابینت (خلاصه)"),
        ("Safe",          "درِ سیف"),
        ("VandalShield",  "شیلد ضدخرابکاری"),
        ("FrontCabinet",  "درِ جلو کابینت"),
        ("RearCabinet",   "درِ عقب کابینت"),
        ("LeftCabinet",   "درِ چپ کابینت"),
        ("RightCabinet",  "درِ راست کابینت"),
        ("TopCabinet",    "درِ بالای کابینت"),
        ("Door8",         "در ۸"),
        ("Door9",         "در ۹"),
        ("Door10",        "در ۱۰"),
        ("Door11",        "در ۱۱"),
        ("Door12",        "در ۱۲"),
        ("Door13",        "در ۱۳"),
        ("Door14",        "در ۱۴"),
        ("Door15",        "در ۱۵"),
    };

        // Doors value map (رایج؛ در صورت تفاوت Vendor اصلاح کنید)
        private static readonly Dictionary<ushort, string> DoorsValueMap = new()
    {
        { 0, "ناموجود/نامشخص" },
        { 1, "بسته/قفل" },     // برخی Vendorها این را "Closed" یا "Locked" می‌گذارند
        { 2, "باز" },
        { 3, "قفل‌شده" },
        { 4, "پلمب/گوه‌گذاری‌شده" },
        { 5, "دستکاری‌شده/تامپر" }
    };

        // Indicators index → title (نمونه‌های متداول)
        private static readonly (string Key, string TitleFa)[] IndicatorSlots = new[]
        {
        ("OpenClose",     "نمایشگر باز/بسته"),
        ("FasciaLight",   "نور فاشیا"),
        ("Audio",         "صدا/بوق"),
        ("Heating",       "گرمایش"),
        ("DisplayBacklight","بک‌لایت نمایشگر مشتری"),
        ("Signage",       "تابلوی تبلیغاتی/Signage"),
        ("Gpo0",          "خروجی عمومی ۰"),
        ("Gpo1",          "خروجی عمومی ۱"),
        ("Gpo2",          "خروجی عمومی ۲"),
        ("Gpo3",          "خروجی عمومی ۳"),
        ("Gpo4",          "خروجی عمومی ۴"),
        ("Gpo5",          "خروجی عمومی ۵"),
        ("Gpo6",          "خروجی عمومی ۶"),
        ("Gpo7",          "خروجی عمومی ۷"),
        ("Ind14",         "ایندیکیتور ۱۴"),
        ("Ind15",         "ایندیکیتور ۱۵"),
    };

        // Indicators value map (برای ساده‌سازی: 0=خاموش/ناموجود، 1=روشن؛
        // اگر Audio/Signage حالت‌های پیچیده‌تری دارند، بعداً با بیت‌فلگ تکمیل کنید)
        private static readonly Dictionary<ushort, string> IndicatorValueMap = new()
    {
        { 0, "خاموش/ناموجود" },
        { 1, "روشن" },
        { 2, "روشن (حالت ۲)" },
        { 3, "روشن (حالت ۳)" }
    };

        // Auxiliaries index → title (نمونه‌های متداول)
        private static readonly (string Key, string TitleFa)[] AuxSlots = new[]
        {
        ("Volume",        "شدت صدا (۱..۱۰۰۰)"),
        ("UPS",           "منبع تغذیه بدون وقفه (UPS)"),
        ("RsmLeds",       "چراغ‌های پایش از راه دور (RSM)"),
        ("AudibleAlarm",  "آژیر/بوق هشدار"),
        ("AudioMode",     "حالت صدای پیشرفته (Public/Private/…)"),
        ("Aux5",          "Aux 5"),
        ("Aux6",          "Aux 6"),
        ("Aux7",          "Aux 7"),
        ("Aux8",          "Aux 8"),
        ("Aux9",          "Aux 9"),
        ("Aux10",         "Aux 10"),
        ("Aux11",         "Aux 11"),
        ("Aux12",         "Aux 12"),
        ("Aux13",         "Aux 13"),
        ("Aux14",         "Aux 14"),
        ("Aux15",         "Aux 15"),
    };

        // برای Aux: بعضی‌ها مقدار عددی/بیت‌فلگ دارند؛ فعلاً ساده‌سازی:
        private static string ToAuxValueFa(int index, ushort raw)
        {
            return index switch
            {
                0 => raw == 0 ? "بی‌صدا" : raw.ToString(), // Volume
                1 => DecodeUpsFlags(raw),
                2 => DecodeRsmFlags(raw),
                _ => raw == 0 ? "خاموش/ناموجود" : $"مقدار: {raw}"
            };
        }

        // Guid Lights index → title (نمونه متداول)
        private static readonly (string Key, string TitleFa)[] GuidSlots = new[]
        {
        ("CardReader",    "چراغ راهنمای کارت"),
        ("Receipt",       "چراغ رسید"),
        ("CashOut",       "چراغ خروجی وجه"),
        ("Envelope",      "چراغ پاکت/چک"),
        ("Printer",       "چراغ پرینتر"),
        ("Passbook",      "چراغ دفترچه"),
        ("DepModule",     "چراغ ماژول واریز"),
        ("Escrow",        "چراغ اسکرو"),
        ("Guid8",         "چراغ ۸"),
        ("Guid9",         "چراغ ۹"),
        ("Guid10",        "چراغ ۱۰"),
        ("Guid11",        "چراغ ۱۱"),
        ("Guid12",        "چراغ ۱۲"),
        ("Guid13",        "چراغ ۱۳"),
        ("Guid14",        "چراغ ۱۴"),
        ("Guid15",        "چراغ ۱۵"),
    };

        // Guid value map (ساده: 0=خاموش، 1=روشن ثابت، 2..4=چشمک‌زن)
        private static readonly Dictionary<ushort, string> GuidValueMap = new()
    {
        { 0, "خاموش" },
        { 1, "روشن ثابت" },
        { 2, "چشمک آهسته" },
        { 3, "چشمک متوسط" },
        { 4, "چشمک سریع" }
        // اگر رنگ/جهت پشتیبانی می‌شود (GuidLightsEx)، اینجا توسعه بدهید.
    };

        public static SiuModuleViewModel ToPersian(this SiuStatusModel m)
        {
            var vm = new SiuModuleViewModel
            {
                DeviceStatusFa = DeviceMap.TryGetValue(m.Device, out var dv) ? dv : $"کد دستگاه: {m.Device}"
            };

            // Doors
            for (int i = 0; i < 16; i++)
            {
                var (key, title) = DoorSlots[i];
                var raw = SafeAt(m.Doors, i);
                vm.Doors.Add(new ItemFa
                {
                    Index = i,
                    Key = key,
                    TitleFa = title,
                    Raw = raw,
                    ValueFa = DoorsValueMap.TryGetValue(raw, out var s) ? s : $"کد: {raw}"
                });
            }

            // Indicators
            for (int i = 0; i < 16; i++)
            {
                var (key, title) = IndicatorSlots[i];
                var raw = SafeAt(m.Indicators, i);
                vm.Indicators.Add(new ItemFa
                {
                    Index = i,
                    Key = key,
                    TitleFa = title,
                    Raw = raw,
                    ValueFa = IndicatorValueMap.TryGetValue(raw, out var s) ? s : $"کد: {raw}"
                });
            }

            // Auxiliaries
            for (int i = 0; i < 16; i++)
            {
                var (key, title) = AuxSlots[i];
                var raw = SafeAt(m.Auxiliaries, i);
                vm.Auxiliaries.Add(new ItemFa
                {
                    Index = i,
                    Key = key,
                    TitleFa = title,
                    Raw = raw,
                    ValueFa = ToAuxValueFa(i, raw)
                });
            }

            // GuidLights
            for (int i = 0; i < 16; i++)
            {
                var (key, title) = GuidSlots[i];
                var raw = SafeAt(m.GuidLights, i);
                vm.GuidLights.Add(new ItemFa
                {
                    Index = i,
                    Key = key,
                    TitleFa = title,
                    Raw = raw,
                    ValueFa = GuidValueMap.TryGetValue(raw, out var s) ? s : $"کد: {raw}"
                });
            }

            return vm;
        }

        private static ushort SafeAt(ushort[] arr, int i) => (arr != null && i >= 0 && i < arr.Length) ? arr[i] : (ushort)0;

        /* ------- نمونه دیکودرهای ساده‌ی UPS/RSM (قابل‌گسترش بر اساس Vendor) ------- */

        private static string DecodeUpsFlags(ushort raw)
        {
            // مثال: بیت‌ها را به برچسب تبدیل کنید (در صورت تفاوت مستند Vendor اصلاح کنید)
            // 0: N/A, 1: Available, 2: Low, 4: Engaged, 8: Powering, 16: Recovered
            if (raw == 0) return "ناموجود/نامشخص";
            var parts = new List<string>();
            if ((raw & 1) != 0) parts.Add("موجود");
            if ((raw & 2) != 0) parts.Add("کمبود باتری");
            if ((raw & 4) != 0) parts.Add("در مدار");
            if ((raw & 8) != 0) parts.Add("تأمین توان");
            if ((raw & 16) != 0) parts.Add("بازیابی‌شده");
            return parts.Count == 0 ? $"کد: {raw}" : string.Join(" + ", parts);
        }

        private static string DecodeRsmFlags(ushort raw)
        {
            // مثال: بیت‌فلگ چراغ‌های RSM (Green/Amber/Red روشن/خاموش)
            if (raw == 0) return "خاموش/ناموجود";
            var parts = new List<string>();
            if ((raw & 1) != 0) parts.Add("سبز روشن");
            if ((raw & 2) != 0) parts.Add("کهربایی روشن");
            if ((raw & 4) != 0) parts.Add("قرمز روشن");
            // توسعه: چشمک/الگوها…
            return parts.Count == 0 ? $"کد: {raw}" : string.Join(" + ", parts);
        }
    }
}
