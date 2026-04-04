namespace HealthRecord.API.Models.Entities;

public class LabResultDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HealthRecordId { get; set; }          // FK → HealthRecords (many:1)
    public int? UserLabItemId { get; set; }          // FK → UserLabItems (nullable)
    public string ItemCode { get; set; } = default!; // r7.8 NHI code
    public string ItemName { get; set; } = default!; // r7.10 sub-item name
    public bool IsNumeric { get; set; } = true;
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }           // qualitative: '-', '1+', 'Pale yellow'
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }      // r7.12 raw format
    public string? NhiOrderName { get; set; }        // r7.9 full order name
    public string? NhiRawValue { get; set; }         // r7.11 raw value

    public User User { get; set; } = default!;
    public HealthRecord HealthRecord { get; set; } = default!;
    public UserLabItem? UserLabItem { get; set; }
}
