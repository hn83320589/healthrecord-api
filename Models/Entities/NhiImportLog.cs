namespace HealthRecord.API.Models.Entities;

public class NhiImportLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime ImportedAt { get; set; }
    public string DataDate { get; set; } = default!;
    public DateOnly DateRangeStart { get; set; }
    public DateOnly DateRangeEnd { get; set; }
    public int HealthRecordCount { get; set; }
    public int MedicationCount { get; set; }
    public int LabCount { get; set; }
    public int SkippedLabs { get; set; }

    public User User { get; set; } = default!;
}
