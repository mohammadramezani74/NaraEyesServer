using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    public class DeviceRestartReport
    {
        public string Ip { get; set; } = null!;
        public string RestartTime { get; set; }
        public string? RestartedBy { get; set; }
        public bool IsSuccess { get; set; }
        public string? ResetAt { get; set; }
    }
}
