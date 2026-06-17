using NaraEyes.Application.Contracts.Models.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.License
{
    public interface ILicenseValidationService
    {
        Task<bool> IsLicenseValidAsync();
        Task<LicenseInfo> GetLicenseInfoAsync();
        Task<bool> RenewLicenseAsync(string newLicenseToken);
    }
}
