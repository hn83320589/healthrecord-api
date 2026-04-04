namespace HealthRecord.API.Models.Entities;

public class MedicationDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HealthRecordId { get; set; }          // FK → HealthRecords (many:1)
    public int? VisitDetailId { get; set; }          // FK → VisitDetails (nullable)
    public string MedicationName { get; set; } = default!; // r1_1.2
    public string? GenericName { get; set; }
    public string? NhiDrugCode { get; set; }         // r1_1.1 (10-char drug code)
    public decimal? Quantity { get; set; }           // r1_1.3
    public decimal? Copayment { get; set; }          // r1_1.4
    public string? Dosage { get; set; }              // manual input
    public string? Frequency { get; set; }           // QD | BID | TID
    public string? Route { get; set; }               // PO | IV
    public int? DurationDays { get; set; }
    public bool IsActive { get; set; }               // Phase 2
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public User User { get; set; } = default!;
    public HealthRecord HealthRecord { get; set; } = default!;
    public VisitDetail? VisitDetail { get; set; }
}
