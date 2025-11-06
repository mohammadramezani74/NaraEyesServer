using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Ptr
{
    public static class PtrHelper
    {
        public static string GetMediaStatus(ushort media) => media switch
        {
            0 => "مدیا حاضر است",                 // WFS_PTR_MEDIAPRESENT
            1 => "مدیا وجود ندارد",               // WFS_PTR_MEDIANOTPRESENT
            2 => "مدیا گیر کرده است",            // WFS_PTR_MEDIAJAMMED
            3 => "مدیا پشتیبانی نمی‌شود",        // WFS_PTR_MEDIANOTSUPP
            4 => "وضعیت مدیا نامشخص است",        // WFS_PTR_MEDIAUNKNOWN
            5 => "مدیا در حال ورود/خروج است",    // WFS_PTR_MEDIAENTERING
            6 => "مدیا به محفظه‌ی Retain رفته است", // WFS_PTR_MEDIARETRACTED
            _ => $"({media}) مقدار ناشناخته"
        };


        public static string GetTonerStatus(ushort toner) => toner switch
        {
            0 => "پر",                 // WFS_PTR_TONERFULL
            1 => "کم",                 // WFS_PTR_TONERLOW
            2 => "تمام شده",           // WFS_PTR_TONEROUT
            3 => "پشتیبانی نمی‌شود",   // WFS_PTR_TONERNOTSUPP
            4 => "نامشخص",             // WFS_PTR_TONERUNKNOWN
            _ => $"({toner}) مقدار ناشناخته"
        };


        public static string GetInkStatus(ushort ink) => ink switch
        {
            0 => "پر",                 // WFS_PTR_INKFULL
            1 => "کم",                 // WFS_PTR_INKLOW
            2 => "تمام شده",           // WFS_PTR_INKOUT
            3 => "پشتیبانی نمی‌شود",   // WFS_PTR_INKNOTSUPP
            4 => "نامشخص",             // WFS_PTR_INKUNKNOWN
            _ => $"({ink}) مقدار ناشناخته"
        };
    }
}
