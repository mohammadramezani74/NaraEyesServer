using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class CashUnitConfig : IEntityTypeConfiguration<CashUnit>
    {
        public void Configure(EntityTypeBuilder<CashUnit> builder)
        {
            builder.ToTable("CashUnits");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Currency)
                   .HasMaxLength(10)
                   .IsRequired();

            builder.Property(x => x.Serial)
                   .HasMaxLength(100)
                   .IsRequired();

    
            builder.Property(x => x.CurrentCount)
                   .HasMaxLength(50);
            builder.Property(x => x.TotalCount)
                   .HasMaxLength(50);

            builder.Property(x => x.Status)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Denomination)
                   .IsRequired();


            builder.ConfigureBaseEntity();
            builder.HasIndex(x => new { x.DeviceId, x.Denomination })
                   .HasDatabaseName("IX_CashUnit_Device_Denomination");
        }
    }
    }
