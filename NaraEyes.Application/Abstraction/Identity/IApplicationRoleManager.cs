using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.Identity
{
    public interface IApplicationRoleManager
    {
        Task<OperationResult> CreateRole(string Name, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> UpdateRole(string name, string newName, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> DeleteRole(string Name, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> CreateRoleClaimsAsync(Guid RoleId, List<string> Claims, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> DeleteRoleClaimsAsync(Guid roleId, string claimName, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<GetRolesResponse>> GetRoles(string? search, CancellationToken cancellationToken = default(CancellationToken));
        Task<string?> GetRoleReport(CancellationToken cancellationToken = default);
        Task<UserRolesResponse[]> GetRolesByUserId(Guid UserId, CancellationToken cancellationToken = default(CancellationToken));
        Task<OperationResult> AddUserToRole(Guid RoleId, Guid UserId, CancellationToken cancellationToken = default(CancellationToken));
        Task<List<string>> GetClaims(Guid RoleId, CancellationToken cancellationToken = default(CancellationToken));

    }
}
