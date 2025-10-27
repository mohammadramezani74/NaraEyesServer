using NaraEyes.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.Identity
{
    public interface IApplicationUserManager
    {
        Guid? UserId { get; }
        Task<User?> GetUserBy(string username, string password);
        Task<User?> GetUserBy(Guid Id);
    }
}
