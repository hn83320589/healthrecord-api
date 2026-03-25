using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Lab;

namespace HealthRecord.API.Services.Interfaces;

public interface ILabService
{
    Task<PagedResult<LabResultResponse>> GetListAsync(int userId, int page, int pageSize, DateTime? from, DateTime? to, string? itemCode);
    Task<LabResultResponse> GetByIdAsync(int userId, int id);
    Task<List<LabResultResponse>> CreateAsync(int userId, CreateLabResultsRequest request);
    Task<LabResultResponse> UpdateAsync(int userId, int id, UpdateLabResultRequest request);
    Task DeleteAsync(int userId, int id);
    Task<List<LabResultsByDateGroup>> GetByDateAsync(int userId, DateTime? from, DateTime? to);
    Task<List<LabTrendPoint>> GetTrendAsync(int userId, string itemCode);
}
