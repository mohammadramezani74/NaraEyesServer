using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Idc
{
    public class IdcModuleViewModel
    {
        public string Device { get; set; }
        public string Media { get; set; }
        public string RetainBin { get; set; }
        public string ChipPower { get; set; }
        public string? LastUpdate { get; set; }
        public DateTime[]? Times { get; set; }
        public string[]? Lables { get; set; }
        public int[]? status { get; set; }
    }
}
