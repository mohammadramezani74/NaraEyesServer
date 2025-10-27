using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class DeviceModuleConfig : IEntityTypeConfiguration<DeviceModule>
    {
        public void Configure(EntityTypeBuilder<DeviceModule> builder)
        {
            builder.ToTable("DeviceModules");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasOne(x => x.Device)
                   .WithMany(d => d.Modules)
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

  
            builder.HasIndex(x => new { x.DeviceId, x.Type })
                   .IsUnique()
                   .HasDatabaseName("UX_DeviceModule_Device_Type");
            builder.ConfigureBaseEntity();
        }
    }
}
