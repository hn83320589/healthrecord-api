using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Medication;

namespace HealthRecord.API.Services.Interfaces;

public interface IMedicationService
{
    Task<PagedResult<MedicationResponse>> GetListAsync(int userId, int page, int pageSize);
    Task<MedicationResponse> GetByIdAsync(int userId, int id);
    Task<MedicationResponse> CreateAsync(int userId, CreateMedicationRequest request);
    Task<MedicationResponse> UpdateAsync(int userId, int id, UpdateMedicationRequest request);
    Task DeleteAsync(int userId, int id);
    Task<List<MedicationResponse>> GetActiveAsync(int userId);
}
