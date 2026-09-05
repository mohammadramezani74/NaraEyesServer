using System.ComponentModel.DataAnnotations;

namespace NaraEyes.Domain.Enumerations
{
    public enum HardwareChangeKind
    {
        /// <summary>ظرفیت یا توان کمتر شده — همان چیزی که بانک شکایت دارد</summary>
        [Display(Name = "تنزل")]
        Downgrade = 1,

        /// <summary>ظرفیت یکسان، قطعه متفاوت — تعویض مشروع، ولی ثبت شود</summary>
        [Display(Name = "تعویض")]
        Replaced = 2,

        [Display(Name = "ارتقا")]
        Upgrade = 3,
    }
}
