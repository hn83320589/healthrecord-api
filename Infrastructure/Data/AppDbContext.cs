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
    public DbSet<VisitDetail> VisitDetails => Set<VisitDetail>();
    public DbSet<MedicationDetail> MedicationDetails => Set<MedicationDetail>();
    public DbSet<NhiImportLog> NhiImportLogs => Set<NhiImportLog>();
    public DbSet<UserLabItem> UserLabItems => Set<UserLabItem>();
    public DbSet<SymptomLog> SymptomLogs => Set<SymptomLog>();
    public DbSet<MedicationReminder> MedicationReminders => Set<MedicationReminder>();
    public DbSet<MedicationLog> MedicationLogs => Set<MedicationLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── Users ────────────────────────────────────────────────────
        m.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        // ── EmergencyContacts ────────────────────────────────────────
        m.Entity<EmergencyContact>()
            .HasOne(c => c.User).WithMany(u => u.EmergencyContacts)
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        // ── HealthRecords (unified record table) ─────────────────────
        m.Entity<Models.Entities.HealthRecord>(e =>
        {
            e.HasOne(h => h.User).WithMany(u => u.HealthRecords)
                .HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(h => h.NhiImportLog).WithMany(n => n.HealthRecords)
                .HasForeignKey(h => h.NhiImportLogId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(h => new { h.UserId, h.RecordType }).HasDatabaseName("idx_user_type");
            e.HasIndex(h => new { h.UserId, h.RecordedAt }).HasDatabaseName("idx_user_date");
            e.HasIndex(h => h.NhiImportLogId).HasDatabaseName("idx_import_log");
        });

        // ── BloodPressureDetails (1:1 with HealthRecord) ────────────
        m.Entity<BloodPressureDetail>(e =>
        {
            e.HasOne(b => b.User).WithMany(u => u.BloodPressures)
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.HealthRecord).WithOne(h => h.BloodPressureDetail)
                .HasForeignKey<BloodPressureDetail>(b => b.HealthRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(b => b.HealthRecordId).IsUnique();
        });

        // ── VisitDetails (1:1 with HealthRecord) ────────────────────
        m.Entity<VisitDetail>(e =>
        {
            e.HasOne(v => v.User).WithMany(u => u.VisitDetails)
                .HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(v => v.HealthRecord).WithOne(h => h.VisitDetail)
                .HasForeignKey<VisitDetail>(v => v.HealthRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(v => v.HealthRecordId).IsUnique();
        });

        // ── LabResultDetails (many:1 with HealthRecord) ─────────────
        m.Entity<LabResultDetail>(e =>
        {
            e.HasOne(l => l.User).WithMany(u => u.LabResults)
                .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.HealthRecord).WithMany(h => h.LabResults)
                .HasForeignKey(l => l.HealthRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.UserLabItem).WithMany(i => i.LabResults)
                .HasForeignKey(l => l.UserLabItemId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(l => l.HealthRecordId).HasDatabaseName("idx_lab_record");
            e.HasIndex(l => new { l.ItemCode, l.ItemName }).HasDatabaseName("idx_lab_item");
        });

        // ── MedicationDetails (many:1 with HealthRecord) ────────────
        m.Entity<MedicationDetail>(e =>
        {
            e.HasOne(md => md.User).WithMany(u => u.Medications)
                .HasForeignKey(md => md.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(md => md.HealthRecord).WithMany(h => h.Medications)
                .HasForeignKey(md => md.HealthRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(md => md.VisitDetail).WithMany(v => v.Medications)
                .HasForeignKey(md => md.VisitDetailId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(md => md.HealthRecordId).HasDatabaseName("idx_med_record");
        });

        // ── NhiImportLogs ───────────────────────────────────────────
        m.Entity<NhiImportLog>(e =>
        {
            e.HasOne(n => n.User).WithMany(u => u.NhiImportLogs)
                .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(n => new { n.UserId, n.FileHash })
                .IsUnique().HasDatabaseName("uq_user_hash");
        });

        // ── UserLabItems ────────────────────────────────────────────
        m.Entity<UserLabItem>(e =>
        {
            e.HasOne(i => i.User).WithMany(u => u.UserLabItems)
                .HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => new { i.UserId, i.ItemCode, i.ItemName })
                .IsUnique().HasDatabaseName("uq_user_item");
        });

        // ── SymptomLogs ────────────────────────────────────────────
        m.Entity<SymptomLog>(e =>
        {
            e.HasOne(s => s.User).WithMany(u => u.SymptomLogs)
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(s => new { s.UserId, s.LoggedAt }).HasDatabaseName("idx_symptom_user_date");
            e.HasIndex(s => new { s.UserId, s.SymptomType }).HasDatabaseName("idx_symptom_user_type");
        });

        // ── MedicationReminders ─────────────────────────────────────
        m.Entity<MedicationReminder>(e =>
        {
            e.HasOne(r => r.User).WithMany(u => u.MedicationReminders)
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.MedicationDetail).WithMany()
                .HasForeignKey(r => r.MedicationDetailId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(r => new { r.UserId, r.IsEnabled }).HasDatabaseName("idx_reminder_user_enabled");
        });

        // ── MedicationLogs ──────────────────────────────────────────
        m.Entity<MedicationLog>(e =>
        {
            e.HasOne(l => l.User).WithMany(u => u.MedicationLogs)
                .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Reminder).WithMany(r => r.Logs)
                .HasForeignKey(l => l.ReminderId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(l => new { l.UserId, l.ScheduledAt }).HasDatabaseName("idx_log_user_date");
            e.HasIndex(l => new { l.ReminderId, l.ScheduledAt }).HasDatabaseName("idx_log_reminder");
            e.HasIndex(l => new { l.UserId, l.Status, l.ScheduledAt }).HasDatabaseName("idx_log_status");
        });

        // ── Global UTC converter ────────────────────────────────────
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
