using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.License
{
    public class LicenseInfo
    {
        public string? LicenseId { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
