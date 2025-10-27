using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Identity
{
    public class Role : IdentityRole<Guid>
    {
        public ICollection<IdentityUserRole<Guid>> UserRoles { get; } = new List<IdentityUserRole<Guid>>();
    }
}
