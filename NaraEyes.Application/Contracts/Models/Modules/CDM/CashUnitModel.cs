using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public class CashUnitModel
    {
        public string? UnitId { get; set; }
        public string? currency { get; set; }
        public uint Init { get; set; }
        public uint Count { get; set; }
        public uint Presented { get; set; }
        public int Denomination { get; set; }
    }
}
