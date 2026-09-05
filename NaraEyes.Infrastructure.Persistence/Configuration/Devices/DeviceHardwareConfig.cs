using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public class DeviceHardwareProfileConfig : IEntityTypeConfiguration<DeviceHardwareProfile>
    {
        public void Configure(EntityTypeBuilder<DeviceHardwareProfile> builder)
        {
            builder.ToTable("DeviceHardwareProfiles");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RamSignature).HasMaxLength(512);
            builder.Property(x => x.CpuName).HasMaxLength(200);
            builder.Property(x => x.CpuId).HasMaxLength(64);
            builder.Property(x => x.DiskModel).HasMaxLength(200);
            builder.Property(x => x.DiskSerial).HasMaxLength(128);
            builder.Property(x => x.BoardManufacturer).HasMaxLength(200);
            builder.Property(x => x.BoardProduct).HasMaxLength(200);
            builder.Property(x => x.BoardSerial).HasMaxLength(128);
            builder.Property(x => x.BiosVersion).HasMaxLength(128);

            builder.HasOne(x => x.Device)
                   .WithMany()
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // یک پروفایل به‌ازای هر دستگاه — یکتایی در سطح دیتابیس تضمین
            // می‌شود، نه فقط در کد. اگر دو پیام همزمان از یک دستگاه برسد،
            // دیتابیس جلوی ساخت دو مبنا را می‌گیرد.
            builder.HasIndex(x => x.DeviceId)
                   .IsUnique()
                   .HasDatabaseName("IX_DeviceHardwareProfiles_Device");
        }
    }

    public class DeviceHardwareChangeConfig : IEntityTypeConfiguration<DeviceHardwareChange>
    {
        public void Configure(EntityTypeBuilder<DeviceHardwareChange> builder)
        {
            builder.ToTable("DeviceHardwareChanges");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Component).HasConversion<int>().IsRequired();
            builder.Property(x => x.Kind).HasConversion<int>().IsRequired();

            builder.Property(x => x.OldValue).HasMaxLength(512);
            builder.Property(x => x.NewValue).HasMaxLength(512);
            builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();

            builder.HasOne(x => x.Device)
                   .WithMany()
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.DeviceId, x.DetectedAt })
                   .HasDatabaseName("IX_DeviceHardwareChanges_Device");

            builder.HasIndex(x => new { x.DetectedAt, x.Component })
                   .HasDatabaseName("IX_DeviceHardwareChanges_Window");
        }
    }
}