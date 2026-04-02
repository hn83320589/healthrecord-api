namespace HealthRecord.API.Models.Entities;

public class LabResultDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? HealthRecordId { get; set; }
    public int? NhiImportLogId { get; set; }
    public DateTime RecordedAt { get; set; }
    // Item definition (denormalized from lab item presets)
    public string ItemName { get; set; } = default!;
    public string ItemCode { get; set; } = default!;
    public string Unit { get; set; } = default!;
    public string Category { get; set; } = default!;
    public decimal? NormalMin { get; set; }
    public decimal? NormalMax { get; set; }
    // Result
    public bool IsNumeric { get; set; } = true;
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public bool IsAbnormal { get; set; }
    // NHI raw data (debug)
    public string? NhiRawValue { get; set; }
    public string? NhiRawRange { get; set; }
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public HealthRecord? HealthRecord { get; set; }
    public NhiImportLog? NhiImportLog { get; set; }
}
