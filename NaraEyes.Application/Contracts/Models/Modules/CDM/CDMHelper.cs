using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public static class CDMHelper
    {
        public static string MapDeviceStatusToPersian(ushort status)
        {
            return status switch
            {
            0 => "آنلاین",
                1 => "آفلاین ",
               2 => "خاموش / بدون برق ",
                3 => "دستگاه یافت نشد",
                4 => "خطای سخت‌افزاری ",
                5 => "خطای کاربری ",
               6 => "مشغول / در حال پردازش ",
               7 => "تلاش برای تقلب (FRAUD ATTEMPT)",
               8=> "احتمال تقلب / نیاز به بررسی (POTENTIAL FRAUD)",
                _ => $"وضعیت ناشناخته ({status})"
            };
        }

        public static string MapSafeDoorStatus(ushort status)
        {
            return status switch
            {
                0 => "سنسور درِ گاوصندوق پشتیبانی نمی‌شود",
                2 => "درِ گاوصندوق باز است",
                1 => "درِ گاوصندوق بسته است",
                3 => "وضعیت درِ گاوصندوق نامشخص",
                _ => "دسترسی قطع شده است"
            };
        }

        public static string MapDispenserStatus(ushort status)
        {
            return status switch
            {
                0 => "سالم و آمادهٔ کار",
                1 => "اشکال در واحدهای نقدی (CUSTATE)",
                2 => "متوقف شده (STOPPED)",
                3 => "وضعیت نامشخص",
                _ => "پشتیبانی نمی‌شود"
            };
        }

        public static string MapStackerStatus(ushort status)
        {
            return status switch
            {
                0 => "خالی",
                1 => "دارای اسکناس",
                2 => "پر / نیاز به تخلیه",
                3 => "وضعیت نامشخص",
                _ => "پشتیبانی نمی‌شود"
            };
        }
    }
}
