namespace HealthRecord.API.Models.Entities;

public class BloodPressureDetail
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HealthRecordId { get; set; }          // UNIQUE FK → HealthRecords (1:1)
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int? Pulse { get; set; }
    public string? MeasurementPosition { get; set; } // sitting | standing | lying
    public string? Arm { get; set; }                 // left | right

    public User User { get; set; } = default!;
    public HealthRecord HealthRecord { get; set; } = default!;
}
