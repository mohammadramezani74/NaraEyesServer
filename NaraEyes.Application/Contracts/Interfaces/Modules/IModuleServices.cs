using NaraEyes.Application.Contracts.Models.Modules;
using NaraEyes.Application.Contracts.Models.Modules.Cam;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Contracts.Models.Modules.Idc;
using NaraEyes.Application.Contracts.Models.Modules.Pin;
using NaraEyes.Application.Contracts.Models.Modules.Ptr;
using NaraEyes.Application.Contracts.Models.Modules.SIU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Modules
{
    public  interface IModuleServices
    {
        /// <summary>
        /// استاتوس تمامی ماژول ها
        /// </summary>
        /// <param name="DeviceIp"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<XfsModule>> GetModulesStatus(string DeviceIp, CancellationToken cancellationToken = default);
        /// <summary>
        /// اطلاعات دیسپنسر
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CdmModuleViewModel> GetCdmInfoAndChart(Guid ModuleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// اطلاعات کارت خوان
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
         Task<IdcModuleViewModel> GetIdcInfo(Guid ModuleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// دریافت اطلاعات پین پد
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PinStatusViewModel> GetPinInfo(Guid ModuleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// دریافت اطلاعات پرینتر
        /// </summary>
        /// <param name="moduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<PtrModuleViewModel> GetPtrInfo(Guid moduleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// دریافت اطلاعات دوربین
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CameraStatusViewModel>GetCameraInfo(Guid ModuleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// دریافت اطلاعات سنسور ها
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<SiuModuleViewModel> GetSiuInfo(Guid ModuleId, CancellationToken cancellationToken = default);
        /// <summary>
        /// دریافت اطلاعات کست ها
        /// </summary>
        /// <param name="DeviceIp"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<Cassette>> GetCassetInfo(string DeviceIp, CancellationToken cancellationToken = default);
    }
}
