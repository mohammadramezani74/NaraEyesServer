using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Idc
{
    public static class IdcHelper
    {
       public static string GetMediaText(ushort v) => v switch
        {
            1 => "کارت داخل دستگاه است",
            2 => "کارت وجود ندارد",
            3 => "کارت گیر کرده است",
            4 => "پشتیبانی نمی‌شود",
            5 => "نامشخص",
            6 => "در حال ورود/خروج",
            7 => "در حالت قفل‌شده",
            _ => $"({v}) ناشناخته"
        };
      public static  string GetRetainBinText(int v) => v switch
        {
            1 => "محفظه نگهداری خالی یا طبیعی است",
            2 => "این دستگاه قابلیت ضبط کارت ندارد",
            3 => "محفظه نگهداری پر است",
            4 => "محفظه نگهداری تقریباً پر شده است",
            _ => $"({v}) مقدار ناشناخته"
        };


        public static string GetChipPowerText(ushort v) => v switch
        {
            0 => "تراشه فعال و آماده است",                // WFS_IDC_CHIPONLINE
            1 => "تراشه وجود دارد اما خاموش است",         // WFS_IDC_CHIPPOWEREDOFF
            2 => "تراشه روشن اما مشغول است",              // WFS_IDC_CHIPBUSY
            3 => "کارت وجود دارد ولی تراشه ندارد",        // WFS_IDC_CHIPNODEVICE
            4 => "خطای سخت‌افزاری در تراشه (MUTE یا مشابه)", // WFS_IDC_CHIPHWERROR
            5 => "هیچ کارتی در دستگاه نیست",             // WFS_IDC_CHIPNOCARD
            6 => "گزارش وضعیت تراشه پشتیبانی نمی‌شود",   // WFS_IDC_CHIPNOTSUPP
            7 => "وضعیت تراشه نامشخص است",               // WFS_IDC_CHIPUNKNOWN
            _ => $"({v}) مقدار ناشناخته"
        };
    }
}
