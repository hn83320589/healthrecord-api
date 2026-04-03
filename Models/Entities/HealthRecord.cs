namespace HealthRecord.API.Models.Entities;

public class HealthRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RecordType { get; set; } = default!; // blood_pressure | lab_result | visit | medication
    public DateTime RecordedAt { get; set; }
    public string? Note { get; set; }
    public string Source { get; set; } = "manual"; // manual | nhi_import | healthkit
    public int? NhiImportLogId { get; set; }
    public string? NhiInstitution { get; set; }
    public string? NhiInstitutionCode { get; set; }
    public string? NhiVisitSeq { get; set; }
    public DateOnly? NhiResultDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = default!;
    public NhiImportLog? NhiImportLog { get; set; }
    public BloodPressureDetail? BloodPressureDetail { get; set; }
    public VisitDetail? VisitDetail { get; set; }
    public ICollection<LabResultDetail> LabResults { get; set; } = [];
    public ICollection<MedicationDetail> Medications { get; set; } = [];
}
