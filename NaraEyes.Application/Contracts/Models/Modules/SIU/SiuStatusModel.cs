using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.SIU
{
    public sealed class SiuStatusModel
    {
        public ushort Device { get; set; }
        public ushort[] Doors { get; set; }
        public ushort[] Indicators { get; set; }
        public ushort[] Auxiliaries { get; set; }
        public ushort[] GuidLights { get; set; }
    }
}
