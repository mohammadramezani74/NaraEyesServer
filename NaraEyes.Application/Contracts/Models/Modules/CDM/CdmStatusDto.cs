using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public sealed class CdmStatusDto
    {
        [Display(Name = "وضعیت کلی ماژول")]
        public ushort Device { get; set; }

        [Display(Name = "وضعیت دیسپنسر")]
        public ushort Dispenser { get; set; }

        [Display(Name = "وضعیت استَکر")]
        public ushort IntermediateStacker { get; set; }

        [Display(Name = "وضعیت درِ گاوصندوق")]
        public ushort SafeDoor { get; set; }

    }
}
