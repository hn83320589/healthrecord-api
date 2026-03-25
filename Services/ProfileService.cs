using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Profile;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class ProfileService(AppDbContext db) : IProfileService
{
    public async Task<ProfileResponse> GetProfileAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return MapProfile(user);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.DisplayName = request.DisplayName;
        user.BirthDate = request.BirthDate;
        user.Gender = request.Gender;
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.BloodType = request.BloodType;
        user.ChronicConditions = request.ChronicConditions;
        user.Allergies = request.Allergies;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapProfile(user);
    }

    public async Task<List<EmergencyContactResponse>> GetEmergencyContactsAsync(int userId)
    {
        return await db.EmergencyContacts
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.SortOrder)
            .Select(c => MapContact(c))
            .ToListAsync();
    }

    public async Task<EmergencyContactResponse> CreateEmergencyContactAsync(int userId, CreateEmergencyContactRequest request)
    {
        var contact = new EmergencyContact
        {
            UserId = userId,
            Name = request.Name,
            Relationship = request.Relationship,
            Phone = request.Phone,
            Note = request.Note,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        db.EmergencyContacts.Add(contact);
        await db.SaveChangesAsync();
        return MapContact(contact);
    }

    public async Task<EmergencyContactResponse> UpdateEmergencyContactAsync(int userId, int contactId, UpdateEmergencyContactRequest request)
    {
        var contact = await db.EmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId)
            ?? throw new KeyNotFoundException("Emergency contact not found.");

        contact.Name = request.Name;
        contact.Relationship = request.Relationship;
        contact.Phone = request.Phone;
        contact.Note = request.Note;
        contact.SortOrder = request.SortOrder;

        await db.SaveChangesAsync();
        return MapContact(contact);
    }

    public async Task DeleteEmergencyContactAsync(int userId, int contactId)
    {
        var contact = await db.EmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId)
            ?? throw new KeyNotFoundException("Emergency contact not found.");

        db.EmergencyContacts.Remove(contact);
        await db.SaveChangesAsync();
    }

    private static ProfileResponse MapProfile(Models.Entities.User user) => new(
        user.Id, user.Email, user.DisplayName, user.BirthDate,
        user.Gender, user.HeightCm, user.WeightKg, user.BloodType,
        user.ChronicConditions, user.Allergies);

    private static EmergencyContactResponse MapContact(EmergencyContact c) => new(
        c.Id, c.Name, c.Relationship, c.Phone, c.Note, c.SortOrder);
}
