namespace HealthRecord.API.Models.DTOs.BloodPressure;

public record BloodPressureResponse(
    int Id,
    int HealthRecordId,
    DateTime RecordedAt,
    int Systolic,
    int Diastolic,
    int? Pulse,
    string? MeasurementPosition,
    string? Arm,
    string Source,
    string? Note,
    DateTime CreatedAt);

public record CreateBloodPressureRequest(
    DateTime RecordedAt,
    int Systolic,
    int Diastolic,
    int? Pulse,
    string? MeasurementPosition,
    string? Arm,
    string? Note);

public record UpdateBloodPressureRequest(
    DateTime RecordedAt,
    int Systolic,
    int Diastolic,
    int? Pulse,
    string? MeasurementPosition,
    string? Arm,
    string? Note);

public record BloodPressureStatsResponse(
    double AvgSystolic,
    double AvgDiastolic,
    double AvgPulse,
    int MaxSystolic,
    int MinSystolic,
    int MaxDiastolic,
    int MinDiastolic,
    Dictionary<string, int> CategoryDistribution,
    int TotalCount);

public record BloodPressureChartPoint(DateTime RecordedAt, int Systolic, int Diastolic, int? Pulse);
