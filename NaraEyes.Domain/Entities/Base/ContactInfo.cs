using NaraEyes.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Base
{
    public sealed class ContactInfo:BaseEntity
    {
        public string? Name { get; private set; }
        public string? Tel { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string? Address { get; private set; }
        public string? Email { get;private set; }
        public static ContactInfo Build(string? name, string? tel, string phoneNumber, string address, string email)
            => new ContactInfo
            {
                Id=Guid.NewGuid()
                ,CreateDate=DateTime.Now,
                Deleted=false,
                Name = name,
                Tel = tel,
                PhoneNumber = phoneNumber,
                Address = address,
                Email = email
            };
    }
}
