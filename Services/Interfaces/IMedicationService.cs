using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;

namespace HealthRecord.API.Services.Interfaces;

public interface IMedicationService
{
    Task<PagedResult<MedicationResponse>> GetListAsync(int userId, int page, int pageSize, string? drugType);
    Task<List<MedicationResponse>> GetCurrentAsync(int userId);
    Task DeleteAsync(int userId, int id);
}
