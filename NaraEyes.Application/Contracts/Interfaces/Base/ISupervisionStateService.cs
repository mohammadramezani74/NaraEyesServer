using NaraEyes.Application.Contracts.Models.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Base
{
    public interface ISupervisionStateService
    {
        Task<List<SupervisionStateViewModel>> GetSupervisionStates(CancellationToken cancellationToken = default);
        Task<SupervisionStateViewModel> GetSupervisionStateByIdAsync(Guid Id, CancellationToken cancellationToken = default);
        Task<OperationResult> CreateAsync(CreateSupervisionStateViewModel model, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(SupervisionStateViewModel model, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    }
}
