using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Domain.Enumerations
{
    /// <summary>
    /// منبع فایلی که از دستگاه درخواست می‌شود.
    ///
    /// ⚠️ مقادیر باید دقیقاً با NaraEyesAgent.Core.Models.Basic.FileSourceType
    /// در ایجنت یکی باشند. این enum روی سیم منتقل می‌شود (به‌صورت عدد در
    /// Payload)، پس تغییر یک مقدار بدون تغییر طرف مقابل یعنی دستگاه فایل
    /// اشتباه برمی‌گرداند — و چون خطا نمی‌دهد، دیر متوجه می‌شوی.
    /// </summary>
    public enum FileSourceType
    {
        [Display(Name = "ژورنال (قدیمی)")]
        LegacyEjournal = 0,

        [Display(Name = "ژورنال ارمغان")]
        ArmaghanJournal = 1,

        [Display(Name = "لاگ سپنتا")]
        SepantaLog = 2,

        [Display(Name = "تصاویر ارمغان")]
        ArmaghanImages = 3,
    }
}