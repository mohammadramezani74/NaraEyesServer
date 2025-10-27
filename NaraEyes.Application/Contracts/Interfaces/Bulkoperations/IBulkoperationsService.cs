using Microsoft.AspNetCore.Components.Forms;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Bulkoperations;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Bulkoperations
{
    public interface IBulkoperationsService
    {
        Task<List<GroupedDeviceViewModel>> GetDevices(GroupedDeviceFilterViewModel filter, CancellationToken cancellationToken = default);
        Task<OperationResult> BulkFileUpload(IBrowserFile file, List<GroupedDeviceViewModel> SelectedDevice,string baseuri, CancellationToken cancellationToken = default);
        Task<string> CreateExcelFileAsync( List<GroupedDeviceViewModel> SelectedDevice, OperationType type, CancellationToken cancellationToken = default);
        Task<OperationResult> RestartSelectedDevices( List<GroupedDeviceViewModel> SelectedDevice, CancellationToken cancellationToken = default);
    }
}
