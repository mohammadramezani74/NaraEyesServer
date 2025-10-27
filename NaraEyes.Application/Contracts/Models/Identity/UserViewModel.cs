using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Identity
{
    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LastLoginDate { get; set; }
        public string? Role { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

    }

}
