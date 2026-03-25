namespace HealthRecord.API.Models.Entities;

public class HealthRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime ClinicDate { get; set; }
    public string? Hospital { get; set; }
    public string? HospitalCode { get; set; }
    public string? VisitSeq { get; set; }
    public string? PrimaryIcdCode { get; set; }
    public string? PrimaryDiagnosis { get; set; }
    public string? SecondaryDiagnoses { get; set; }
    public int? Copay { get; set; }
    public int? TotalPoints { get; set; }
    public string Source { get; set; } = "manual";
    public int? NhiImportLogId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public NhiImportLog? NhiImportLog { get; set; }
    public ICollection<MedicationDetail> Medications { get; set; } = [];
    public ICollection<LabResultDetail> LabResults { get; set; } = [];
    public ICollection<BloodPressureDetail> BloodPressures { get; set; } = [];
}
