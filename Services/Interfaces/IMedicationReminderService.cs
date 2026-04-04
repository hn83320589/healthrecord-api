using HealthRecord.API.Models.DTOs.MedicationReminder;

namespace HealthRecord.API.Services.Interfaces;

public interface IMedicationReminderService
{
    Task<List<ReminderResponse>> GetAllAsync(int userId);
    Task<ReminderResponse> CreateAsync(int userId, CreateReminderRequest request);
    Task<ReminderResponse> CreateFromMedicationAsync(int userId, int medicationDetailId, CreateReminderFromMedRequest? request);
    Task<ReminderResponse> UpdateAsync(int userId, int id, UpdateReminderRequest request);
    Task DeleteAsync(int userId, int id);
    Task<ReminderResponse> ToggleAsync(int userId, int id);
}
