using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class DeviceSupplyConfig : IEntityTypeConfiguration<DeviceSupply>
    {
        public void Configure(EntityTypeBuilder<DeviceSupply> builder)
        {
            builder.ToTable("DeviceSupplies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.LevelPercent);
            builder.Property(x => x.Count);


            builder.HasOne(x => x.Module)
                   .WithMany() 
                   .HasForeignKey(x => x.DeviceModuleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.DeviceModuleId, x.Type })
                   .IsUnique()
                   .HasDatabaseName("UX_DeviceSupply_Module_Type");


            builder.HasIndex(x => x.ModifiedDate)
                   .HasDatabaseName("IX_DeviceSupply_ModifiedDate");
        }
    }
    }
