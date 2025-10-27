using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Identity
{
    public record UserRolesResponse(Guid RoleId, string Name);
}
