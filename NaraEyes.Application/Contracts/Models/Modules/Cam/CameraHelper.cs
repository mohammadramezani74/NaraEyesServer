using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Cam
{
    public static class CameraHelper
    {
        public static string MapCameraState(ushort state) => state switch
        {
            0 => "این دوربین پشتیبانی نمی‌شود",
            1 => "دوربین سالم و آمادهٔ کار است",
            2 => "دوربین از کار افتاده است",
            3 => "وضعیت دوربین نامشخص است",
            _ => "وضعیت دوربین نامعتبر/نامشخص"
        };

        /// <summary>
        /// مپ وضعیت رسانه/حافظه ضبط برای هر دوربین (WFSCAMSTATUS.fwMedia[i])
        /// 0=OK, 1=High(threshold), 2=Full, 3=Unknown, 4=NotSupported
        /// </summary>
        public static string MapMediaState(ushort media) => media switch
        {
            0 => "حافظهٔ ضبط در وضعیت عادی است",
            1 => "حافظه نزدیک پر شدن (هشدار سطح بالا)",
            2 => "حافظهٔ ضبط پُر است",
            3 => "وضعیت حافظه نامشخص است",
            4 => "تشخیص سطح حافظه پشتیبانی نمی‌شود",
            _ => "وضعیت حافظه نامعتبر/نامشخص"
        };

        /// <summary>
        /// مپ اندیس دوربین‌ها طبق XFS: 0=ROOM, 1=PERSON, 2=EXITSLOT
        /// (برای نمایش فارسیِ برچسب‌ها)
        /// </summary>
        public static string MapCameraIndexToFa(string index) => index switch
        {
            "ROOM" => "دوربین محیط (ROOM)",
            "PERSON" => "دوربین مشتری (PERSON)",
            "EXITSLOT" => "دوربین دهانهٔ خروج (EXITSLOT)",
            _ => $"دوربین ناشناخته ({index})"
        };

        /// <summary>
        /// اگر لِیبِل خام دستگاه را داری (مثلاً "ROOM", "PERSON", "EXITSLOT") به متن فارسی تبدیل کن.
        /// فاصله‌های اضافی را هم Trim می‌کند.
        /// </summary>
        public static string MapLabelFa(string? rawLabel)
        {
            var label = (rawLabel ?? string.Empty).Trim().ToUpperInvariant();
            return label switch
            {
                "ROOM" => "دوربین محیط (ROOM)",
                "PERSON" => "دوربین مشتری (PERSON)",
                "EXITSLOT" => "دوربین دهانهٔ خروج (EXITSLOT)",
                _ => string.IsNullOrWhiteSpace(label) ? "دوربین نامشخص" : $"دوربین نامشخص ({label})"
            };
        }

        /// <summary>
        /// مپ شمارش تصاویر ذخیره‌شده به نمایش خوانا با جداکنندهٔ هزارگان
        /// </summary>
        public static string MapPicturesCount(uint pictures)
            => pictures.ToString("#,0");

        /// <summary>
        /// اختیاری/فروشنده-محور: اگر AntiFraudModule را به‌صورت 0/1/2... می‌فرستی،
        /// این نگاشت را طبق قرارداد داخلی خودت اصلاح کن.
        /// </summary>
        public static string MapAntiFraudModuleStatus(ushort status) => status switch
        {
            0 => "ماژول ضدتقلب تعریف نشده/غیرفعال",
            1 => "ماژول ضدتقلب فعال و سالم",
            2 => "خطای ماژول ضدتقلب",
            _ => "وضعیت ضدتقلب نامشخص"
        };
    }
}
