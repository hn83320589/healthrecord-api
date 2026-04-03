using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Visit;

namespace HealthRecord.API.Services.Interfaces;

public interface IVisitService
{
    Task<PagedResult<VisitResponse>> GetListAsync(int userId, int page, int pageSize, DateTime? from, DateTime? to);
    Task<VisitDetailResponse> GetByIdAsync(int userId, int id);
    Task<VisitResponse> CreateAsync(int userId, CreateVisitRequest request);
    Task<VisitResponse> UpdateAsync(int userId, int id, UpdateVisitRequest request);
    Task DeleteAsync(int userId, int id);
}
