using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class DeviceEventConfig : IEntityTypeConfiguration<DeviceEvent>
    {
        public void Configure(EntityTypeBuilder<DeviceEvent> builder)
        {
            builder.ToTable("DeviceEvents");

            builder.HasKey(x => x.Id);


            builder.HasOne(x => x.Device)
                   .WithMany(d => d.Events)
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.EventTime)
                   .IsRequired();

            builder.Property(x => x.Severity)
                   .HasConversion<int>() 
                   .IsRequired();

            builder.Property(x => x.Module)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.Code)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Message)
                   .HasMaxLength(1000)
                   .IsRequired();

            builder.Property(x => x.PayloadJson)
                   .HasColumnType("nvarchar(max)"); 

            builder.Property(x => x.Acknowledged)
                   .IsRequired();

    builder.HasOne(c=>c.AcknowledgedBy)
                .WithMany()
                .HasForeignKey(c => c.AcknowledgedById)
               .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => new { x.DeviceId, x.EventTime })
                   .HasDatabaseName("IX_DeviceEvent_Device_Time");


            builder.HasIndex(x => x.Severity)
                   .HasDatabaseName("IX_DeviceEvent_Severity");
            builder.ConfigureBaseEntity();
        }
    }
}
