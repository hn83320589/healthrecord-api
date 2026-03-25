namespace HealthRecord.API.Models.Entities;

public class MedicationDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? HealthRecordId { get; set; }
    public int? NhiImportLogId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string DrugName { get; set; } = default!;
    public string? NhiDrugCode { get; set; }
    public decimal? Quantity { get; set; }
    public int? Days { get; set; }
    public string DrugType { get; set; } = "medication";
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public HealthRecord? HealthRecord { get; set; }
    public NhiImportLog? NhiImportLog { get; set; }
}
