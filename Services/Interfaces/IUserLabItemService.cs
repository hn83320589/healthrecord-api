using HealthRecord.API.Models.DTOs.UserLabItem;

namespace HealthRecord.API.Services.Interfaces;

public interface IUserLabItemService
{
    Task<List<UserLabItemResponse>> GetAllAsync(int userId);
    Task<UserLabItemResponse> CreateAsync(int userId, CreateUserLabItemRequest request);
    Task<UserLabItemResponse> UpdateAsync(int userId, int id, UpdateUserLabItemRequest request);
    Task DeleteAsync(int userId, int id);
}
