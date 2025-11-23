using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public sealed class MetricSnapshotConfig : IEntityTypeConfiguration<MetricSnapshot>
    {
        public void Configure(EntityTypeBuilder<MetricSnapshot> builder)
        {
            builder.ToTable("MetricSnapshots");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.CapturedAt)
                   .IsRequired();




         
            builder.Property(x => x.NetworkLatencyMs);
            builder.Property(x => x.PingOk)
                   .IsRequired();

       
            builder.Property(x => x.AgentAlive)
                   .IsRequired();
            builder.Property(x => x.AgentVersion)
                   .HasMaxLength(50);
            builder.Property(x => x.CpuModel)
            .HasMaxLength(150);
            builder.Property(x => x.OsInfo)
        .HasMaxLength(200);


            builder.Property(x => x.ExtraJson)
                   .HasColumnType("nvarchar(max)");

            builder.HasIndex(x => new { x.DeviceId, x.CapturedAt })
                   .HasDatabaseName("IX_MetricSnapshot_Device_Time");
            builder.ConfigureBaseEntity();

        }
    }
}
