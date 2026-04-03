namespace HealthRecord.API.Models.Entities;

public class VisitDetail
{
    public int Id { get; set; }
    public int HealthRecordId { get; set; }
    public string? VisitType { get; set; }
    public string? VisitTypeCode { get; set; }      // r1.1
    public string? Department { get; set; }
    public string? DoctorName { get; set; }
    public string? DiagnosisCode1 { get; set; }      // r1.8
    public string? DiagnosisName1 { get; set; }      // r1.9
    public string? DiagnosisCode2 { get; set; }      // r1.10
    public string? DiagnosisName2 { get; set; }      // r1.11
    public string? DiagnosisCode3 { get; set; }      // r1.14 (skip 12/13)
    public string? DiagnosisName3 { get; set; }      // r1.15
    public string? DiagnosisCode4 { get; set; }      // r1.16
    public string? DiagnosisName4 { get; set; }      // r1.17
    public string? DiagnosisCode5 { get; set; }      // r1.18
    public string? DiagnosisName5 { get; set; }      // r1.19
    public string? CopaymentCode { get; set; }       // r1.12
    public decimal? MedicalCost { get; set; }        // r1.13
    public string? NhiRawData { get; set; }          // full JSON for debug

    public HealthRecord HealthRecord { get; set; } = default!;
    public ICollection<MedicationDetail> Medications { get; set; } = [];
}
