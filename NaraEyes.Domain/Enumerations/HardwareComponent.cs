using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum HardwareComponent
    {
        [Display(Name = "حافظه")]
        Ram = 1,

        [Display(Name = "پردازنده")]
        Cpu = 2,

        [Display(Name = "هارد دیسک")]
        Disk = 3,

        [Display(Name = "مادربرد")]
        Motherboard = 4,
    }
}
