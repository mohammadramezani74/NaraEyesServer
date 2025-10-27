using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Identity
{
    public class User : IdentityUser<Guid>
    {
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime? LastLoginDate { get; private set; }
        public ICollection<IdentityUserRole<Guid>> UserRoles { get; } = new List<IdentityUserRole<Guid>>();
        public void SetName(string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(FirstName));
            }

            if (lastName.Length > 100)
            {
                throw new ArgumentException("LastName cannot exceed 100 characters.", nameof(LastName));
            }

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
        }
        public void SetActive(bool isactive)=>IsActive = isactive;
        public void SetLastLoginDate()
        {
            LastLoginDate = DateTime.Now;
        }
    }
}
