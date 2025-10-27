using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Base
{
    public class Branch : BaseEntity
    {
        public string Name { get;private set; } = null!;
        public string? ShortName { get;private set; }
        public int Code { get;private set; }
        public Guid SupervisionId { get;private set; }
        public SupervisionState Supervision { get;private set; }
        public string? Address { get;private set; }
        public string? PostalCode { get;private set; }
        public string? Phone { get;private set; }
        public decimal? Latitude { get;private set; }
        public decimal? Longitude { get;private set; }
        public bool IsActive { get;private set; } = true;
        public  ICollection<Device> Devices { get; private set; }=new List<Device>();
  
     
            public static Branch Create(
                string name,
                int code,
                Guid supervisionId,
                 Guid UserId,
                string? shortName = null,
                string? address = null,
                string? postalCode = null,
                string? phone = null,
                decimal? latitude = null,
                decimal? longitude = null
               )
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Branch name cannot be empty", nameof(name));

                if (code <= 0)
                    throw new ArgumentOutOfRangeException(nameof(code), "Branch code must be greater than zero");
            name = name.Trim();
            shortName = string.IsNullOrWhiteSpace(shortName) ? null : shortName.Trim();
            address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            postalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
            phone = string.IsNullOrWhiteSpace(phone) ? null : NormalizePhone(phone!);
            return new Branch
                {
                    Name = name.Trim(),
                    Code = code,
                    SupervisionId = supervisionId,
                    ShortName = shortName?.Trim(),
                    Address = address?.Trim(),
                    PostalCode = postalCode?.Trim(),
                    Phone = phone?.Trim(),
                    Latitude = latitude,
                    Longitude = longitude,
                    IsActive = true,
                    CreatedByUserId=UserId
                };
            }
            public void UpdateInfo(Guid UserId,string name, string? shortName, string? address, string? postalCode, string? phone, decimal? latitude,decimal? longtitude)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Branch name cannot be empty", nameof(name));

                Name = name.Trim();
                ShortName = shortName?.Trim();
                Address = address?.Trim();
                PostalCode = postalCode?.Trim();
                Phone = phone?.Trim();
            Latitude = latitude;
            Longitude = longtitude;
            ModifiedDate = DateTime.Now;
            ModifiedById = UserId;
            }
        public void SetCode(int code)
        {
            Code = code;
        }
        public void SetSupervisionId(Guid supId)
        {
         SupervisionId=supId;
        }

        public void SetLocation(decimal? latitude, decimal? longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }

            public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
        private static string NormalizePhone(string input)
        {

            var s = input.Trim();
            s = s
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty);
            s = s
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
            return s;
        }
    }
}
