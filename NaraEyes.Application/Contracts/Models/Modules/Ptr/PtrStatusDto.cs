using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Ptr
{
    public class PtrStatusDto
    {
        public ushort Device { get; set; }
        public ushort Media { get; set; }
        public ushort Toner { get; set; }
        public ushort Ink { get; set; }
        public PaperStatus Paper { get; set; }
    }
}
