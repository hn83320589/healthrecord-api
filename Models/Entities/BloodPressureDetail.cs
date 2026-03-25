namespace HealthRecord.API.Models.Entities;

public class BloodPressureDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? HealthRecordId { get; set; }
    public int? NhiImportLogId { get; set; }
    public DateTime RecordedAt { get; set; }
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int Pulse { get; set; }
    public string? MeasurementPosition { get; set; }
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public HealthRecord? HealthRecord { get; set; }
    public NhiImportLog? NhiImportLog { get; set; }
}
