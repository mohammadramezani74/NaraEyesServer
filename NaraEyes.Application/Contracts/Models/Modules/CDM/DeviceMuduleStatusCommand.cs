using NaraEyes.Application.Contracts.Models.Modules.Cam;
using NaraEyes.Application.Contracts.Models.Modules.Idc;
using NaraEyes.Application.Contracts.Models.Modules.Pin;
using NaraEyes.Application.Contracts.Models.Modules.Ptr;
using NaraEyes.Application.Contracts.Models.Modules.SIU;

namespace NaraEyes.Application.Contracts.Models.Modules.CDM
{
    public class DeviceMuduleStatusCommand
    {
        public string DeviceIp { get; set; } = null!;
        public CdmStatusDto? CdmStatus { get; set; }
        public IdcStatusDto IdcStatus { get; set; }
        public PtrStatusDto ptrStatus { get; set; }
        public CameraStatusDto CameraStatus { get; set; }
        public PinStatusDto PinStatus { get; set; }
        public SiuStatusModel SiuStatus { get; set; }
        public List<CashUnitModel> Cashunit { get; set; } = new();
    }
}
