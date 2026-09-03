using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaraEyes.Domain.Entities.Devices;

namespace NaraEyes.Infrastructure.Persistence.Configuration.Devices
{
    public class ModuleFaultLogConfig : IEntityTypeConfiguration<ModuleFaultLog>
    {
        public void Configure(EntityTypeBuilder<ModuleFaultLog> builder)
        {
            builder.ToTable("ModuleFaultLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Module).HasConversion<int>().IsRequired();
            builder.Property(x => x.StartStatus).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentStatus).HasConversion<int>().IsRequired();

            builder.Property(x => x.Detail).HasMaxLength(300);
            builder.Property(x => x.StartedAt).IsRequired();

            builder.HasOne(x => x.Device)
                   .WithMany()
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // پرتکرارترین کوئری: یافتن بازه‌ی باز یک ماژول.
            // فیلتر جزئی روی ResolvedAt چون فقط ردیف‌های باز جستجو می‌شوند
            // و تعدادشان بسیار کمتر از کل جدول است.
            builder.HasIndex(x => new { x.DeviceId, x.Module, x.ResolvedAt })
                   .HasDatabaseName("IX_ModuleFaultLogs_Open");

            // گزارش‌گیری بر اساس بازه‌ی زمانی
            builder.HasIndex(x => new { x.StartedAt, x.Module })
                   .HasDatabaseName("IX_ModuleFaultLogs_Report");

            // فیلتر بر اساس دستگاه
            builder.HasIndex(x => new { x.DeviceId, x.StartedAt })
                   .HasDatabaseName("IX_ModuleFaultLogs_Device");
        }
    }
}