using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum DeviceMode
    {
        [Display(Name ="در حال سرویس دهی")]
        InService=1,
        [Display(Name = "خارج از سرویس")]
        Supervisor =2,
        [Display(Name = "هشدار")]
        warning =3,
        [Display(Name = "خطا")]
        Error =4,
        [Display(Name = "آفلاین")]
        Offline =5,
        [Display(Name = "آنلاین")]
        Online =6,
        [Display(Name = " (هشدار (کمبود کاغذ")]
        warning_paper = 7,
        [Display(Name = " (هشدار (کمبود پول")]
        warning_Money = 8,

    }
}
