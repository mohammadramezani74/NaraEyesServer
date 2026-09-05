using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public class ServerUptimeLogConfig : IEntityTypeConfiguration<ServerUptimeLog>
    {
        public void Configure(EntityTypeBuilder<ServerUptimeLog> builder)
        {
            builder.ToTable("ServerUptimeLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StartedAt).IsRequired();
            builder.Property(x => x.LastAliveAt).IsRequired();
            builder.Property(x => x.Version).HasMaxLength(50);

            builder.HasIndex(x => x.StartedAt)
                   .HasDatabaseName("IX_ServerUptimeLogs_Started");
        }
    }
}