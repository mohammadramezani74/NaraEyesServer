using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Idc
{
    public class IdcStatusDto
    {
        public ushort Device { get; set; }
        public ushort Media { get; set; }
        public ushort RetainBin { get; set; }
        public ushort usCards { get; set; }
        public ushort ChipPower { get; set; }

    }
}
