using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Base;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Base
{
    public sealed class ContactInfoConfig : IEntityTypeConfiguration<ContactInfo>
    {
        public void Configure(EntityTypeBuilder<ContactInfo> builder)
        {
            builder.ToTable("ContactInfos");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(200);

            builder.Property(x => x.Tel)
                   .HasMaxLength(20)
                   .IsUnicode(false);

            builder.Property(x => x.PhoneNumber)
                   .HasMaxLength(20)
                   .IsUnicode(false);

            builder.Property(x => x.Address)
                   .HasMaxLength(500);

            builder.Property(x => x.Email)
                   .HasMaxLength(200)
                   .IsUnicode(false);
            builder.ConfigureBaseEntity();

        }
    }
}
