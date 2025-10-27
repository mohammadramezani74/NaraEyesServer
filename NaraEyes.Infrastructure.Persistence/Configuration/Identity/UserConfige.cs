using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Identity
{
    public class UserConfige : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.FirstName)
                  .HasMaxLength(350)
                  .IsUnicode(true);

            builder.Property(u => u.LastName)
                   .HasMaxLength(350)
                  .IsUnicode(true);

            builder.Property(u => u.IsActive).IsRequired();


        }
    }
}
