using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Symptom;

namespace HealthRecord.API.Services.Interfaces;

public interface ISymptomService
{
    Task<PagedResult<SymptomResponse>> GetListAsync(int userId, int page, int pageSize,
        DateTime? startDate, DateTime? endDate, string? type);
    Task<SymptomResponse> GetByIdAsync(int userId, int id);
    Task<SymptomResponse> CreateAsync(int userId, CreateSymptomRequest request);
    Task<SymptomResponse> UpdateAsync(int userId, int id, UpdateSymptomRequest request);
    Task DeleteAsync(int userId, int id);
    Task<SymptomSummaryResponse> GetSummaryAsync(int userId, int months);
    Task<List<string>> GetTypesAsync(int userId);
}
