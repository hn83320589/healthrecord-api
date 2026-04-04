using HealthRecord.API.Models.DTOs.MedicationReminder;

namespace HealthRecord.API.Services.Interfaces;

public interface IMedicationLogService
{
    Task<List<MedicationLogResponse>> GetListAsync(int userId, DateTime? startDate, DateTime? endDate, string? status);
    Task<List<MedicationLogResponse>> GetTodayAsync(int userId);
    Task<MedicationLogResponse> TakeAsync(int userId, int id);
    Task<MedicationLogResponse> SkipAsync(int userId, int id, string? note);
    Task<MedicationLogResponse> UndoAsync(int userId, int id);
    Task<AdherenceResponse> GetAdherenceAsync(int userId, int days);
}
