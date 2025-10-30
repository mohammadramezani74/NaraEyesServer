using Microsoft.AspNetCore.Identity;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Identity
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserViewModel>> AllUsers(CancellationToken cancellationToken);
        Task<OperationResult>CreateUserAsync(CreateUserModel command, CancellationToken cancellationToken);
        Task<OperationResult>UpdateUserAsync(UpdateUserModel command, CancellationToken cancellationToken);
        Task<OperationResult>DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
        Task<OperationResult> ChangePassword(ChangePasswordModel command, CancellationToken cancellationToken);
        Task SigninUser(string username, CancellationToken cts = default);
    }
}
