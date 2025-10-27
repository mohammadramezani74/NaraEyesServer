using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Base;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Base
{
    public sealed class SupervisionStateConfig : IEntityTypeConfiguration<SupervisionState>
    {
        public void Configure(EntityTypeBuilder<SupervisionState> b)
        {
            b.ToTable("SupervisionStates");

            b.Property(x => x.Name).IsRequired().HasMaxLength(150);
            b.Property(x => x.ShortName).IsRequired().HasMaxLength(100);
            b.Property(x => x.Code).IsRequired().HasMaxLength(10).IsUnicode(false);
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Name);
            b.ConfigureBaseEntity();
        }
    }
}
