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
            1 => "موجود",
            2 => "مفقود",
            3 => "گیر کرده",
            4 => "نامشخص",
            _ => "ناشناخته"
        };

     
        public static string GetTonerStatus(ushort toner) => toner switch
        {
            1 => "پر",
            2 => "کم",
            3 => "تمام",
            4 => "نامشخص",
            _ => "ناشناخته"
        };


        public static string GetInkStatus(ushort ink) => ink switch
        {
            1 => "پر",
            2 => "کم",
            3 => "تمام",
            4 => "نامشخص",
            _ => "ناشناخته"
        };
    }
}
