using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public class DeviceStateLogConfig : IEntityTypeConfiguration<DeviceStateLog>
    {
        public void Configure(EntityTypeBuilder<DeviceStateLog> builder)
        {
            builder.ToTable("DeviceStateLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.State).HasConversion<int>().IsRequired();
            builder.Property(x => x.StartMode).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentMode).HasConversion<int>().IsRequired();

            builder.Property(x => x.StartedAt).IsRequired();
            builder.Property(x => x.LastSeenAt).IsRequired();

            builder.HasOne(x => x.Device)
                   .WithMany()
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // پرتکرارترین کوئری با اختلاف زیاد: یافتن بازه‌ی باز یک دستگاه.
            // این در **هر** چرخه‌ی متریک برای **هر** دستگاه اجرا می‌شود،
            // یعنی حدود ۳۰۰ بار هر سه دقیقه. بدون این ایندکس، جدول با
            // گذشت زمان بزرگ می‌شود و اسکن کامل کل مسیر متریک را کند
            // می‌کند — همان اشتباهی که قبلاً با N+1 گرفتار شدیم.
            builder.HasIndex(x => new { x.DeviceId, x.EndedAt })
                   .HasDatabaseName("IX_DeviceStateLogs_Open");

            // گزارش‌گیری روی بازه‌ی زمانی. StartedAt اول می‌آید چون شرط
            // اصلی گزارش روی همان است.
            builder.HasIndex(x => new { x.StartedAt, x.EndedAt })
                   .HasDatabaseName("IX_DeviceStateLogs_Window");

            builder.HasIndex(x => new { x.DeviceId, x.StartedAt })
                   .HasDatabaseName("IX_DeviceStateLogs_Device");
        }
    }
}