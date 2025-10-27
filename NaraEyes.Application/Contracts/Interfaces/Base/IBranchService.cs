using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Base
{
    public interface IBranchService
    {
        Task<List<BranchesViewModel>> GetAllBranches(CancellationToken cancellationToken);
        Task<List<BranchesViewModel>> GetBranchesBySupervisionIdAsync(Guid Id,CancellationToken cancellationToken= default);
        Task<OperationResult>CreateBranchAsync(CreateBranchModel command,CancellationToken cancellationToken);
        Task<OperationResult> UpdateBranchAsync(UpdateBranchModel command,CancellationToken cancellationToken);
        Task<OperationResult> DeleteBranchAsync(Guid Id, CancellationToken cancellationToken);
    }
}
