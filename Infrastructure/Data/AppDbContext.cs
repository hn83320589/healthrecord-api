using HealthRecord.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthRecord.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<Models.Entities.HealthRecord> HealthRecords => Set<Models.Entities.HealthRecord>();
    public DbSet<BloodPressureDetail> BloodPressureDetails => Set<BloodPressureDetail>();
    public DbSet<LabResultDetail> LabResultDetails => Set<LabResultDetail>();
    public DbSet<MedicationDetail> MedicationDetails => Set<MedicationDetail>();
    public DbSet<NhiImportLog> NhiImportLogs => Set<NhiImportLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        m.Entity<Models.Entities.HealthRecord>()
            .HasOne(h => h.User).WithMany(u => u.HealthRecords)
            .HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<Models.Entities.HealthRecord>()
            .HasOne(h => h.NhiImportLog).WithMany()
            .HasForeignKey(h => h.NhiImportLogId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<BloodPressureDetail>()
            .HasOne(b => b.User).WithMany(u => u.BloodPressures)
            .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<BloodPressureDetail>()
            .HasOne(b => b.HealthRecord).WithMany(h => h.BloodPressures)
            .HasForeignKey(b => b.HealthRecordId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<BloodPressureDetail>()
            .HasOne(b => b.NhiImportLog).WithMany()
            .HasForeignKey(b => b.NhiImportLogId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<LabResultDetail>()
            .HasOne(l => l.User).WithMany(u => u.LabResults)
            .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<LabResultDetail>()
            .HasOne(l => l.HealthRecord).WithMany(h => h.LabResults)
            .HasForeignKey(l => l.HealthRecordId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<LabResultDetail>()
            .HasOne(l => l.NhiImportLog).WithMany()
            .HasForeignKey(l => l.NhiImportLogId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<LabResultDetail>()
            .HasIndex(l => new { l.UserId, l.ItemCode });

        m.Entity<LabResultDetail>()
            .HasIndex(l => new { l.NhiCode, l.NhiItemName });

        m.Entity<MedicationDetail>()
            .HasOne(md => md.User).WithMany(u => u.Medications)
            .HasForeignKey(md => md.UserId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<MedicationDetail>()
            .HasOne(md => md.HealthRecord).WithMany(h => h.Medications)
            .HasForeignKey(md => md.HealthRecordId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<MedicationDetail>()
            .HasOne(md => md.NhiImportLog).WithMany()
            .HasForeignKey(md => md.NhiImportLogId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        m.Entity<EmergencyContact>()
            .HasOne(c => c.User).WithMany(u => u.EmergencyContacts)
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<NhiImportLog>()
            .HasOne(n => n.User).WithMany(u => u.NhiImportLogs)
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        // Global UTC converter: all DateTime fields are stored as UTC,
        // and read back with Kind.Utc so comparisons with DateTime.UtcNow are safe.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v == null ? null : v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime(),
            v => v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

        foreach (var entityType in m.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcConverter);
            }
        }
    }
}
