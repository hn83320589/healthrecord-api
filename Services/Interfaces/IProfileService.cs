using HealthRecord.API.Models.DTOs.Profile;

namespace HealthRecord.API.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetProfileAsync(int userId);
    Task<ProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<List<EmergencyContactResponse>> GetEmergencyContactsAsync(int userId);
    Task<EmergencyContactResponse> CreateEmergencyContactAsync(int userId, CreateEmergencyContactRequest request);
    Task<EmergencyContactResponse> UpdateEmergencyContactAsync(int userId, int contactId, UpdateEmergencyContactRequest request);
    Task DeleteEmergencyContactAsync(int userId, int contactId);
}
