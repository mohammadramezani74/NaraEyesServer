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

     
      public static  string GetChipPowerText(int v) => v switch
        {
            1 => "تراشه فعال و آماده است",
            2 => "تراشه وجود دارد اما خاموش است",
            3 => "تراشه روشن اما مشغول است",
            4 => "کارت وجود دارد ولی تراشه ندارد",
            5 => "خطای سخت‌افزاری در تراشه (MUTE یا مشابه)",
            6 => "هیچ کارتی در دستگاه نیست",
            7 => "گزارش وضعیت تراشه پشتیبانی نمی‌شود",
            8 => "وضعیت تراشه نامشخص است",
            _ => $"({v}) مقدار ناشناخته"
        };
    }
}
