using HealthRecord.API.Models.DTOs.Lab;
using HealthRecord.API.Models.DTOs.Medication;

namespace HealthRecord.API.Models.DTOs.HealthRecord;

public record HealthRecordResponse(
    int Id,
    DateTime ClinicDate,
    string? Hospital,
    string? HospitalCode,
    string? PrimaryIcdCode,
    string? PrimaryDiagnosis,
    string? SecondaryDiagnoses,
    int? Copay,
    int? TotalPoints,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record HealthRecordDetailResponse(
    int Id,
    DateTime ClinicDate,
    string? Hospital,
    string? HospitalCode,
    string? PrimaryIcdCode,
    string? PrimaryDiagnosis,
    string? SecondaryDiagnoses,
    int? Copay,
    int? TotalPoints,
    string Source,
    string? Note,
    DateTime CreatedAt,
    List<MedicationResponse> Medications,
    List<LabResultResponse> LabResults);
