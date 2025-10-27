using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Cam
{
    public class CameraStatusDto
    {
        public ushort Device { get; set; }
        public ushort AntiFraudModule { get; set; }
        public List<CameradetailDto> Detailes { get; set; } = new List<CameradetailDto>();
    }
}
