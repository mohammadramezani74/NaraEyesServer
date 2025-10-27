using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public class CdmModuleViewModel
    {
        [Display(Name = "وضعیت کلی ماژول")]
        public string? Device { get; set; }

        [Display(Name = "وضعیت دیسپنسر")]
        public string? Dispenser { get; set; }

        [Display(Name = "وضعیت استَکر")]
        public string? IntermediateStacker { get; set; }

        [Display(Name = "وضعیت درِ گاوصندوق")]
        public string? SafeDoor { get; set; }
        public string? LastUpdate { get; set; }
        public DateTime[]? Times { get; set; }
        public string[]? Lables { get; set; }
        public int []? status { get; set; }
    }
}
