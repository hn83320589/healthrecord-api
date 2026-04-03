namespace HealthRecord.API.Models.Entities;

public class NhiImportLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FileHash { get; set; } = default!; // SHA256 of JSON content
    public string? FileName { get; set; }
    public DateOnly? DataDate { get; set; }          // b1.2
    public int RecordCount { get; set; }
    public int LabCount { get; set; }
    public int VisitCount { get; set; }
    public int MedicationCount { get; set; }
    public int NewItemCount { get; set; }
    public int SkippedLabCount { get; set; }
    public int DuplicateLabCount { get; set; }
    public DateOnly? DateRangeStart { get; set; }
    public DateOnly? DateRangeEnd { get; set; }
    public DateTime ImportedAt { get; set; }

    public User User { get; set; } = default!;
    public ICollection<HealthRecord> HealthRecords { get; set; } = [];
}
