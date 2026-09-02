using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum DeviceVendor
    {
        [Display(Name = "نامشخص")]
        Unknown = 0,

        [Display(Name = "هیوسانگ")]
        Hyosung = 1,

        [Display(Name = "GRG")]
        GRG = 2,

        [Display(Name = "سایر")]
        Other = 99,
    }
}
