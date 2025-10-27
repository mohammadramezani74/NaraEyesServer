using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class DeviceModuleStatusConfig : IEntityTypeConfiguration<DeviceModuleStatus>
    {
        public void Configure(EntityTypeBuilder<DeviceModuleStatus> builder)
        {
            builder.ToTable("DeviceModuleStatuses");

            builder.HasKey(x => x.Id);



            builder.Property(x => x.Status)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.StateJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(x => x.Severity)
                   .IsRequired();

            builder.HasOne(x => x.Module)
                   .WithMany() 
                   .HasForeignKey(x => x.DeviceModuleId)
                   .OnDelete(DeleteBehavior.Cascade);


            builder.HasIndex(x => x.DeviceModuleId)
                   .IsUnique()
                   .HasDatabaseName("UX_ModuleStatus_Module");


            builder.HasIndex(x => x.Severity)
                   .HasDatabaseName("IX_ModuleStatus_Severity");
            builder.ConfigureBaseEntity();
        }
    }

}
