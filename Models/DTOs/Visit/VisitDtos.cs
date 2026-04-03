using HealthRecord.API.Models.DTOs.Medication;

namespace HealthRecord.API.Models.DTOs.Visit;

public record VisitResponse(
    int Id,
    int HealthRecordId,
    DateTime RecordedAt,
    string? NhiInstitution,
    string? NhiInstitutionCode,
    string? VisitType,
    string? VisitTypeCode,
    string? Department,
    string? DoctorName,
    string? DiagnosisCode1,
    string? DiagnosisName1,
    string? DiagnosisCode2,
    string? DiagnosisName2,
    string? DiagnosisCode3,
    string? DiagnosisName3,
    string? CopaymentCode,
    decimal? MedicalCost,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record VisitDetailResponse(
    int Id,
    int HealthRecordId,
    DateTime RecordedAt,
    string? NhiInstitution,
    string? NhiInstitutionCode,
    string? VisitType,
    string? VisitTypeCode,
    string? Department,
    string? DoctorName,
    string? DiagnosisCode1,
    string? DiagnosisName1,
    string? DiagnosisCode2,
    string? DiagnosisName2,
    string? DiagnosisCode3,
    string? DiagnosisName3,
    string? DiagnosisCode4,
    string? DiagnosisName4,
    string? DiagnosisCode5,
    string? DiagnosisName5,
    string? CopaymentCode,
    decimal? MedicalCost,
    string Source,
    string? Note,
    DateTime CreatedAt,
    List<MedicationResponse> Medications);

public record CreateVisitRequest(
    DateTime RecordedAt,
    string? Department,
    string? DoctorName,
    string? DiagnosisCode1,
    string? DiagnosisName1,
    string? Note);

public record UpdateVisitRequest(
    DateTime RecordedAt,
    string? Department,
    string? DoctorName,
    string? DiagnosisCode1,
    string? DiagnosisName1,
    string? Note);
